using FuturesSignalsBot.Indicators;
using FuturesSignalsBot.Indicators.Smoothing;
using FuturesSignalsBot.Models;

namespace FuturesSignalsBot.Services.Analysis;

public static class CryptocurrencyAnalysisEngine
{
    public static void InitializeIndicators(Cryptocurrency cryptocurrency)
    {
        var thirtyMinuteData = cryptocurrency.TradingDataContainer.ThirtyMinuteData;
        var fiveMinuteData = cryptocurrency.TradingDataContainer.FiveMinuteData;
        var lastItem30M = thirtyMinuteData.Last();

        CryptoAnalysisTools.CalculateAllTmoValues(thirtyMinuteData, cryptocurrency.Name is "BTCUSDT");
        lastItem30M.Tmo60 = CryptoAnalysisTools.CalculateLastTmoForTimeFrame(thirtyMinuteData, 60);

        for (var i = fiveMinuteData.Count - 428; i < fiveMinuteData.Count; i++)
        {
            var prices = fiveMinuteData
                .Skip(i - 72 + 1)
                .Take(72)
                .Select(data => data.Low)
                .ToList();

            var score = ZScoreCalculator.CalculateScores(prices);
            fiveMinuteData[i].Score = score;
        }

        LsmaSmoothing.Smooth(cryptocurrency.TradingDataContainer.FourHourData);
        LsmaSmoothing.Smooth(cryptocurrency.TradingDataContainer.ThirtyMinuteData);
        LsmaSmoothing.Smooth(cryptocurrency.TradingDataContainer.FiveMinuteData);
    }

    public static void CalculateLastItemIndicators(Cryptocurrency cryptocurrency)
    {
        var thirtyMinuteData = cryptocurrency.TradingDataContainer.ThirtyMinuteData;
        var lastItem30M = thirtyMinuteData.Last();

        lastItem30M.Tmo30 = CryptoAnalysisTools.CalculateLastTmoForTimeFrame(thirtyMinuteData, 30);
        lastItem30M.Tmo60 = CryptoAnalysisTools.CalculateLastTmoForTimeFrame(thirtyMinuteData, 60);
        lastItem30M.Tmo180 = CryptoAnalysisTools.CalculateLastTmoForTimeFrame(thirtyMinuteData, 180);

        if (cryptocurrency.Name is "BTCUSDT")
        {
            lastItem30M.Tmo240 = CryptoAnalysisTools.CalculateLastTmoForTimeFrame(thirtyMinuteData, 240);
        }
    }

    public static (decimal ZScoreRatio, decimal MinMaxPercentage) CalculateZValues(
        List<CryptocurrencyDataItem> fiveMinuteData, bool isLong)
    {
        var lastItem5M = fiveMinuteData.Last();
        const int maxCandles = 427;
        var triggerPrice = isLong ? lastItem5M.Close * 1.1m : lastItem5M.Close * 0.9m;
        
        var selectedCandles = SelectCandles(fiveMinuteData, isLong, triggerPrice, maxCandles);
        
        decimal zScoreRatio = 0;
        if (selectedCandles.Count > 0)
        {
            var filteredCandles = isLong
                ? [.. selectedCandles.Where(c => c.Score.ZScore < c.Score.InvertedZScore)]
                : selectedCandles.Where(c => c.Score.ZScore > c.Score.InvertedZScore).ToList();

            if (filteredCandles.Count > 0)
            {
                var averageScore = isLong
                    ? filteredCandles.Average(c => c.Score.InvertedZScore)
                    : filteredCandles.Average(c => c.Score.ZScore);
                zScoreRatio = Math.Abs(averageScore) * 100 / 3;
                
            }
        }
        
        var minMaxPercentage = 0m;
        if (selectedCandles.Count > 0)
        {
            var validCandlesCount = 0;
            for (var i = 0; i < selectedCandles.Count; i++)
            {
                var currentCandle = selectedCandles[i];
                var isValid = true;

                for (var j = i; j < selectedCandles.Count; j++)
                {
                    var compareCandle = selectedCandles[j];
                    if (isLong)
                    {
                        if (compareCandle.High > currentCandle.High)
                        {
                            isValid = false;
                            break;
                        }
                    }
                    else
                    {
                        if (compareCandle.Low < currentCandle.Low)
                        {
                            isValid = false;
                            break;
                        }
                    }
                }

                if (isValid)
                {
                    validCandlesCount++;
                }
            }

            minMaxPercentage = (decimal)validCandlesCount * 100 / selectedCandles.Count;
        }

        return (zScoreRatio, minMaxPercentage);
        
        static List<CryptocurrencyDataItem> SelectCandles(List<CryptocurrencyDataItem> data, bool isLong,
            decimal triggerPrice, int maxCandles)
        {
            var selectedCandles = new List<CryptocurrencyDataItem>();
            var count = 0;

            for (var i = data.Count - 1; i >= 0 && count < maxCandles; i--)
            {
                var candle = data[i];
                if (isLong)
                {
                    if (candle.Low < triggerPrice)
                    {
                        selectedCandles.Add(candle);
                        count++;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if (candle.High > triggerPrice)
                    {
                        selectedCandles.Add(candle);
                        count++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return selectedCandles;
        }
    }
}