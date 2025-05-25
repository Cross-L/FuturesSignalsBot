using System;
using System.Collections.Generic;
using System.Linq;
using FuturesSignalsBot.Indicators;
using FuturesSignalsBot.Indicators.Smoothing;
using FuturesSignalsBot.Models;
using FuturesSignalsBot.Models.IndicatorResults;

namespace FuturesSignalsBot.Services.Analysis;

public static class CryptocurrencyAnalysisEngine
{
    public static void InitializeIndicators(Cryptocurrency cryptocurrency)
    {
        var thirtyMinuteData = cryptocurrency.TradingDataContainer.ThirtyMinuteData;
        var lastItem30M = thirtyMinuteData.Last();
        var totalCount = thirtyMinuteData.Count;
        
        var bandwidthResults = BandwidthVolatilityIndicator.CalculateIndicator(thirtyMinuteData.TakeLast(255).ToList());
        lastItem30M.Bandwidth = bandwidthResults.Last();
        
        thirtyMinuteData.Last().CustomVolumeProfileValue = VolumeProfileManager.CalculateCustomVolumeProfileValue
            (thirtyMinuteData, 90, 50m, 24);

        CryptoAnalysisTools.CalculateAllTmoValues(thirtyMinuteData, cryptocurrency.Name is "BTCUSDT");
        lastItem30M.Tmo60 = CryptoAnalysisTools.CalculateLastTmoForTimeFrame(thirtyMinuteData,60);
        
        UpdateRegression(thirtyMinuteData, totalCount, 450, 288, 2.5m, 
            (item, value) => item.Tf5Regression = value);
        
        UpdateRegression(thirtyMinuteData, totalCount, 100, 288, 1.5m, 
            (item, value) => item.Tf3Regression = value);
        
        UpdateRegression(thirtyMinuteData, totalCount, 10, 96, 2m, 
            (item, value) => item.Tf1Regression = value);
        
        for (var i = totalCount - 500; i < totalCount; i++)
        {
            var startIndex = Math.Max(0, i - 300 + 1);
            var count = i - startIndex + 1;
            var window = thirtyMinuteData.GetRange(startIndex, count);
            thirtyMinuteData[i].Frama = FramaIndicator.CalculateFramaSignal(window.ToList());
        }
        
        LsmaSmoothing.Smooth(cryptocurrency.TradingDataContainer.FourHourData);
        LsmaSmoothing.Smooth(cryptocurrency.TradingDataContainer.ThirtyMinuteData);
        LsmaSmoothing.Smooth(cryptocurrency.TradingDataContainer.FiveMinuteData);
    }

    public static void CalculateLastItemIndicators(Cryptocurrency cryptocurrency)
    {
        var thirtyMinuteData = cryptocurrency.TradingDataContainer.ThirtyMinuteData;
        var lastItem30M = thirtyMinuteData.Last();
        
        lastItem30M.CustomVolumeProfileValue = VolumeProfileManager.CalculateCustomVolumeProfileValue
            (thirtyMinuteData, 90, 50m, 24);
        
        lastItem30M.Tmo30 = CryptoAnalysisTools.CalculateLastTmoForTimeFrame(thirtyMinuteData,30);
        lastItem30M.Tmo60 = CryptoAnalysisTools.CalculateLastTmoForTimeFrame(thirtyMinuteData,60);
        lastItem30M.Tmo180 = CryptoAnalysisTools.CalculateLastTmoForTimeFrame(thirtyMinuteData,180);
        
        if (cryptocurrency.Name is "BTCUSDT")
        {
            lastItem30M.Tmo240 = CryptoAnalysisTools.CalculateLastTmoForTimeFrame(thirtyMinuteData,240);
        }
        
        var regression3 = CryptoAnalysisTools.CalculateTfRegression
            (thirtyMinuteData, 288,1.5m);
        lastItem30M.Tf3Regression = regression3;
        
        var regression1 = CryptoAnalysisTools.CalculateTfRegression
            (thirtyMinuteData, 96,2m);
        lastItem30M.Tf1Regression = regression1;
        
        var regression5 = CryptoAnalysisTools.CalculateTfRegression
            (thirtyMinuteData, 288,2.5m);
        lastItem30M.Tf5Regression = regression5;
        
        var bandwidthResults = BandwidthVolatilityIndicator.CalculateIndicator(thirtyMinuteData.TakeLast(255).ToList());
        lastItem30M.Bandwidth = bandwidthResults.Last();
        
        lastItem30M.Frama = FramaIndicator.CalculateFramaSignal(thirtyMinuteData.TakeLast(300).ToList());
    }
    
    private static void UpdateRegression(
        List<CryptocurrencyDataItem> thirtyMinuteData, int totalCount,
        int candleCount, int period, decimal multiplier,
        Action<CryptocurrencyDataItem, LinearRegression> setRegression)
    {
        var startIndex = Math.Max(0, totalCount - candleCount);
        for (var i = startIndex; i < totalCount; i++)
        {
            var regression = CryptoAnalysisTools.CalculateTfRegression(thirtyMinuteData, period, multiplier, i);
            setRegression(thirtyMinuteData[i], regression);
        }
    }
    
}