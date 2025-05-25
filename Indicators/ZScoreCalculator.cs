using System;
using System.Collections.Generic;

namespace FuturesSignalsBot.Indicators;

public static class ZScoreCalculator
{
    private const int SLength = 72;
    private const int Length = 72;

    public static (decimal ZScore, decimal InvertedZScore) CalculateScores(List<decimal> prices)
    {
        var n = prices.Count;
        if (n < Math.Max(SLength, Length))
            throw new ArgumentException($"Price list must contain at least {Math.Max(SLength, Length)} elements.");

        var ma = CalculateWma(prices, Length);
        var stdDev = CalculateStdDev(prices, SLength);
        var src = prices[n - 1];

        if (stdDev == 0)
            return (0m, 0m);

        var zscore = (src - ma) / stdDev;
        var invertedZscore = -zscore;

        return (ZScore: zscore, InvertedZScore: invertedZscore);
    }

    private static decimal CalculateWma(List<decimal> prices, int length)
    {
        var n = prices.Count;
        var sum = 0m;

        for (var i = 1; i <= length; i++)
        {
            var index = n - length + i - 1;
            sum += i * prices[index];
        }

        var denominator = length * (length + 1) / 2m;

        return sum / denominator;
    }

    private static decimal CalculateStdDev(List<decimal> prices, int slength)
    {
        var n = prices.Count;

        if (slength <= 1)
            return 0m;

        var sum = 0m;
        for (var i = n - slength; i < n; i++)
        {
            sum += prices[i];
        }
        var mean = sum / slength;

        var sumOfSquares = 0m;
        for (var i = n - slength; i < n; i++)
        {
            var diff = prices[i] - mean;
            sumOfSquares += diff * diff;
        }

        var variance = sumOfSquares / (slength - 1);

        return Sqrt(variance);
    }

    private static decimal Sqrt(decimal x)
    {
        if (x < 0)
            throw new ArgumentException("Cannot calculate square root of a negative number.");

        if (x == 0)
            return 0m;

        var guess = x / 2m;
        const decimal epsilon = 0.0000000000000000000000000001m;

        while (true)
        {
            var newGuess = (guess + x / guess) / 2m;
            if (Math.Abs(newGuess - guess) < epsilon)
                return newGuess;
            guess = newGuess;
        }
    }
}