using FuturesSignalsBot.Models;

namespace FuturesSignalsBot.Indicators.Smoothing;

public static class LsmaSmoothing
{
    public static void Smooth(List<CryptocurrencyDataItem> inputData, int period = 7)
    {
        var closeValues = inputData.Select(item => item.Close).ToList();

        for (var i = 0; i < closeValues.Count; i++)
        {
            if (i >= period - 1)
            {
                var subset = closeValues.Skip(i - period + 1).Take(period).ToList();
                inputData[i].SmoothedClose = LinearRegression(subset, period);
            }
            else
            {
                inputData[i].SmoothedClose = 0;
            }
        }
    }

    
    public static void SmoothLastItem(List<CryptocurrencyDataItem> inputData, int period = 7)
    {
        if (inputData == null || inputData.Count < period)
            throw new ArgumentException("Input data is too short for the specified period!");

        var closeValues = inputData.Select(item => item.Close).ToList();
        var subset = closeValues.Skip(closeValues.Count - period).ToList();
        var smoothedCloseValue = LinearRegression(subset, period);
        
        var lastItem = inputData.Last();
        lastItem.SmoothedClose = smoothedCloseValue;
    }


    
    private static decimal LinearRegression(IReadOnlyList<decimal> data, int length)
    {
        const int offset = 0;
        decimal sumX = 0, sumY = 0, sumXy = 0, sumX2 = 0;
        
        for (var i = 0; i < length; i++)
        {
            sumX += i + offset;
            sumY += data[i];
            sumXy += (i + offset) * data[i];
            sumX2 += (i + offset) * (i + offset);
        }
        
        var slope = (length * sumXy - sumX * sumY) / (length * sumX2 - sumX * sumX);
        var intercept = (sumY - slope * sumX) / length;
        
        return slope * (length - 1 + offset) + intercept;
    }
}