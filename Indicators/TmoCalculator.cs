namespace FuturesSignalsBot.Indicators;

public static class TmoCalculator
{
    public static double CalculateTmo(List<double> openPrices, List<double> closePrices, 
        int tmoLength = 14, int calcLength = 5, int smoothLength = 3)
    {
        if (openPrices.Count != closePrices.Count)
            throw new ArgumentException("Длины списков openPrices и closePrices должны совпадать.");

        var count = openPrices.Count;
        if (count < tmoLength)
            throw new ArgumentException($"Для расчёта TMO нужно хотя бы {tmoLength} свечей.");

        var data = new double[count];

        for (var i = 0; i < count; i++)
        {
            if (i < tmoLength)
            {
                data[i] = 0;
            }
            else
            {
                double sum = 0;
                for (var j = 1; j < tmoLength; j++)
                {
                    if (closePrices[i] > openPrices[i - j])
                        sum++;
                    else if (closePrices[i] < openPrices[i - j])
                        sum--;
                }
                data[i] = sum;
            }
        }

        var ema1 = CalculateEma(data, calcLength);
        var main = CalculateEma(ema1, smoothLength);
        var lastMain = main[count - 1];
        var tmoMain = 15.0 * lastMain / tmoLength;

        return tmoMain;
    }

    private static double[] CalculateEma(double[] values, int period)
    {
        var result = new double[values.Length];
        if (period <= 1)
        {
            for (var i = 0; i < values.Length; i++)
                result[i] = values[i];
            return result;
        }

        var alpha = 2.0 / (period + 1.0);
        result[0] = values[0];

        for (var i = 1; i < values.Length; i++)
        {
            var prevEma = result[i - 1];
            var currentValue = values[i];
            result[i] = alpha * currentValue + (1 - alpha) * prevEma;
        }

        return result;
    }
}