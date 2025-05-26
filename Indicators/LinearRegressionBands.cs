using FuturesSignalsBot.Models.IndicatorResults;

namespace FuturesSignalsBot.Indicators;

public static class LinearRegressionBands
{
    public static LinearRegression CalculateRegression(List<decimal> prices, int length, decimal deviation)
    {
        if (prices.Count < length)
            throw new ArgumentException("Количество цен меньше, чем length.");

        var window = new decimal[length];
        for (var i = 0; i < length; i++)
        {
            window[i] = prices[prices.Count - 1 - i];
        }

        var sumWeights = 0m;
        var sumWeighted = 0m;
        for (var i = 0; i < length; i++)
        {
            decimal weight = length - i;
            sumWeights += weight;
            sumWeighted += window[i] * weight;
        }
        var wma = sumWeighted / sumWeights;

        var sum = 0m;
        for (var i = 0; i < length; i++)
        {
            sum += window[i];
        }
        var sma = sum / length;

        var a = 4m * sma - 3m * wma;
        var b = 3m * wma - 2m * sma;
        var slope = (a - b) / (length - 1);

        var variance = 0m;
        for (var i = 0; i < length; i++)
        {
            var line = b + slope * i;
            var difference = window[i] - line;
            variance += difference * difference;
        }

        var stdDev = SqrtDecimal(variance / (length - 1));
        var linRegAdjustment = stdDev * deviation;

        var upperBand = (b + linRegAdjustment) - slope;
        var lowerBand = (b - linRegAdjustment) - slope;

        return new LinearRegression(upperBand, lowerBand);
    }

    private static decimal SqrtDecimal(decimal value, int iterations = 24)
    {
        if (value < 0m)
            throw new ArgumentException("Нельзя вычислить корень из отрицательного числа.");
        if (value == 0m)
            return 0m;

        var guess = value < 1m ? value : value / 2m;

        for (int i = 0; i < iterations; i++)
        {
            if (guess == 0m)
                return 0m;
            guess = (guess + value / guess) / 2m;
        }
        return guess;
    }
}