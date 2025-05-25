using System;
using System.Collections.Generic;
using System.Linq;
using FuturesSignalsBot.Models;
using FuturesSignalsBot.Models.IndicatorResults;

namespace FuturesSignalsBot.Indicators;

public static class BandwidthVolatilityIndicator
{
    private const int Length = 7;
    private const int MaLength = 90;
    private const int PrLength = 252;

    private static decimal StDev(List<decimal> values)
    {
        if (values.Count == 0) return 0m;
        var avg = values.Average();
        var sumSq = values.Sum(v => (v - avg) * (v - avg));
        return (decimal)Math.Sqrt((double)(sumSq / values.Count));
    }
        

    private static decimal Sma(List<decimal> values, int length)
    {
        if (length <= 0 || length > values.Count)
            return decimal.MinValue;

        return values.Skip(values.Count - length).Average();
    }

    private static decimal PercentileRank(List<decimal> values, decimal currentValue, int length)
    {
        if (length <= 0 || length > values.Count)
            throw new ArgumentException("Invalid length for percentile rank.");

        var slice = values.Skip(values.Count - length).Take(length).ToList();
        var countLess = slice.Count(v => v < currentValue);
        return ((decimal)countLess / length) * 100m;
    }

    private static decimal BandwidthSilverman(decimal sd, int length)
    {
        const decimal factor = 1.06m;
        var power = Math.Pow(length, -1.0 / 5.0);
        return factor * sd * (decimal)power;
    }

    public static List<BandwidthResult> CalculateIndicator(List<CryptocurrencyDataItem> data)
    {
        var closePrices = data.Select(x => x.Close).ToList();
            
        if (closePrices == null || closePrices.Count < Math.Max(Length, PrLength))
            throw new ArgumentException("Not enough data to calculate the indicator.");

        var logReturns = new List<decimal> { decimal.MinValue };

        for (var i = 1; i < closePrices.Count; i++)
        {
            var lrDouble = Math.Log((double)(closePrices[i] / closePrices[i - 1])) * 100.0;
            var lr = (decimal)lrDouble;
            logReturns.Add(lr);
        }
            
        var bwSilvermanList = new List<decimal>();

        for (var i = 0; i < closePrices.Count; i++)
        {
            if (i >= Length)
            {
                var recentLogReturns = logReturns.Skip(i + 1 - Length).Take(Length).ToList();
                var sd = StDev(recentLogReturns);
                var bwS = BandwidthSilverman(sd, Length);
                bwSilvermanList.Add(bwS);
            }
            else
            {
                bwSilvermanList.Add(decimal.MinValue);
            }
        }

        var results = new List<BandwidthResult>();

        for (var i = 0; i < closePrices.Count; i++)
        {
            var maV = decimal.MinValue;
            var mp = 0m;

            if (i >= Length && i >= PrLength && i >= MaLength)
            {
                var currentBw = bwSilvermanList[i];
                mp = PercentileRank(bwSilvermanList, currentBw, PrLength);
                var usedBwSubList = bwSilvermanList.Take(i + 1).ToList();
                maV = Sma(usedBwSubList, MaLength);
            }

            var chosenBw = bwSilvermanList[i];

            results.Add(new BandwidthResult
            {
                SmoothedBandwidth = maV,
                Bandwidth = chosenBw,
                Mp = mp
            });
        }

        return results;
    }
}