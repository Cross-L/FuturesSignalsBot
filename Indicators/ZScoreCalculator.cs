namespace FuturesSignalsBot.Indicators;

public static class ZScoreCalculator
{
    public static (decimal ZScore, decimal InvertedZScore) CalculateScores(
        IReadOnlyList<decimal> prices, 
        int currentIndex, 
        int averageLength = 12,
        int stdDevLength = 12)
    {
        int requiredLookback = Math.Max(averageLength, stdDevLength);

        if (currentIndex < requiredLookback - 1)
            return (0m, 0m);

        var ma = CalculateWma(prices, currentIndex, averageLength);
        var stdDev = CalculateStdDev(prices, currentIndex, stdDevLength);
        var src = prices[currentIndex];

        if (stdDev == 0)
            return (0m, 0m);

        var zscore = (src - ma) / stdDev;
        var invertedZscore = -zscore;

        return (ZScore: zscore, InvertedZScore: invertedZscore);
    }

    private static decimal CalculateWma(IReadOnlyList<decimal> prices, int currentIndex, int length)
    {
        var sum = 0m;
        var weightSum = 0m;

        for (var i = 1; i <= length; i++)
        {
            var index = currentIndex - length + i;
            sum += i * prices[index];
            weightSum += i;
        }

        return sum / weightSum;
    }

    private static decimal CalculateStdDev(IReadOnlyList<decimal> prices, int currentIndex, int slength)
    {
        if (slength <= 1)
            return 0m;

        var sum = 0m;
        int start = currentIndex - slength + 1;

        for (var i = start; i <= currentIndex; i++)
        {
            sum += prices[i];
        }
        var mean = sum / slength;

        var sumOfSquares = 0m;
        for (var i = start; i <= currentIndex; i++)
        {
            var diff = prices[i] - mean;
            sumOfSquares += diff * diff;
        }

        var variance = sumOfSquares / (slength - 1);
        return Sqrt(variance);
    }

    private static decimal Sqrt(decimal x)
    {
        if (x < 0) throw new ArgumentException("Cannot calculate square root of a negative number.");
        if (x == 0) return 0m;

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