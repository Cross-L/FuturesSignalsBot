using FuturesSignalsBot.Indicators;
using FuturesSignalsBot.Models;
using FuturesSignalsBot.Models.IndicatorResults;

namespace FuturesSignalsBot.Services.Analysis;

public static class CryptoAnalysisTools
{
    public static decimal CalculatePositivePercentageChange(decimal num1, decimal num2)
    {
        var percentageChange = (num2 - num1) / num1 * 100;
        return Math.Abs(percentageChange);
    }
    
    public static decimal CalculatePercentageChange(decimal num1, decimal num2)
    {
        var percentageChange = (num2 - num1) / num1 * 100;
        return percentageChange;
    }
    
    public static LinearRegression CalculateTfRegression(List<CryptocurrencyDataItem> data, int requiredDataCount,
        decimal deviation, int? index = null)
    {
        var currentIndex = index ?? data.Count - 1;
        var startIndex = Math.Max(0, currentIndex - requiredDataCount + 1);
        var window = data.Skip(startIndex)
            .Take(currentIndex - startIndex + 1)
            .ToList();
        var hourlyCloses = window
            .Where((_, idx) => idx % 2 == 0)
            .Select(item => item.Close)
            .ToList();
        return LinearRegressionBands.CalculateRegression(hourlyCloses, requiredDataCount / 2, deviation);
    }

    public static void CalculateAllTmoValues(List<CryptocurrencyDataItem> thirtyMinuteData, bool isBtc)
    {
        for (var i = 0; i < thirtyMinuteData.Count; i++)
        {
            if (i + 1 >= 50)
            {
                var segment30 = thirtyMinuteData.GetRange(i + 1 - 50, 50);
                var tmo30 = TmoCalculator.CalculateTmo(
                    segment30.Select(item => (double)item.Open).ToList(),
                    segment30.Select(item => (double)item.Close).ToList()
                );
                thirtyMinuteData[i].Tmo30 = tmo30;
            }

            if (i + 1 >= 950)
            {
                var segment60 = thirtyMinuteData.GetRange(i + 1 - 100, 100);
                var aggregated60 = AggregateToTimeFrame(segment60, 60);
            
                var tmo60 = TmoCalculator.CalculateTmo(
                    aggregated60.Select(item => (double)item.Open).ToList(),
                    aggregated60.Select(item => (double)item.Close).ToList()
                );
                thirtyMinuteData[i].Tmo60 = tmo60;
            }

            if (i + 1 >= 300)
            {
                var segment180 = thirtyMinuteData.GetRange(i + 1 - 300, 300);
                var aggregated180 = AggregateToTimeFrame(segment180, 180);

                var tmo180 = TmoCalculator.CalculateTmo(
                    aggregated180.Select(item => (double)item.Open).ToList(),
                    aggregated180.Select(item => (double)item.Close).ToList()
                );
                thirtyMinuteData[i].Tmo180 = tmo180;
            }

            if (isBtc)
            {
                if (i + 1 >= 400)
                {
                    var segment240 = thirtyMinuteData.GetRange(i + 1 - 400, 400);
                    var aggregated240 = AggregateToTimeFrame(segment240, 240);

                    var tmo240 = TmoCalculator.CalculateTmo(
                        aggregated240.Select(item => (double)item.Open).ToList(),
                        aggregated240.Select(item => (double)item.Close).ToList()
                    );
                    thirtyMinuteData[i].Tmo240 = tmo240;
                }
            }
        }
    }
    
    public static double CalculateLastTmoForTimeFrame(List<CryptocurrencyDataItem> candleData,
        int aggregateTimeFrame)
    {
        var takeCount = aggregateTimeFrame / 30 * 50;
        var data = candleData
            .TakeLast(takeCount)
            .ToList();

        if (aggregateTimeFrame > 30)
        {
            data = AggregateToTimeFrame(data, aggregateTimeFrame);
        }

        var openPrices = data.Select(item => (double)item.Open).ToList();
        var closePrices = data.Select(item => (double)item.Close).ToList();

        return TmoCalculator.CalculateTmo(openPrices, closePrices);
    }
    
    

    private static List<CryptocurrencyDataItem> AggregateToTimeFrame(List<CryptocurrencyDataItem> thirtyMinuteData,
        int targetTimeFrameInMinutes)
    {
        if (targetTimeFrameInMinutes % 30 != 0)
            throw new ArgumentException("targetTimeFrameInMinutes должен быть кратен 30.");

        var factor = targetTimeFrameInMinutes / 30;
        var result = new List<CryptocurrencyDataItem>();
        var fullGroupsCount = thirtyMinuteData.Count / factor;

        for (var g = 0; g < fullGroupsCount; g++)
        {
            var startIndex = g * factor;
            var chunk = thirtyMinuteData.Skip(startIndex).Take(factor).ToList();

            var aggregatedBar = new CryptocurrencyDataItem
            {
                OpenTime = chunk.First().OpenTime,
                Open = chunk.First().Open,
                Close = chunk.Last().Close,
                High = chunk.Max(b => b.High),
                Low = chunk.Min(b => b.Low),
                Volume = chunk.Sum(b => b.Volume)
            };

            result.Add(aggregatedBar);
        }

        return result;
    }
    
    
}