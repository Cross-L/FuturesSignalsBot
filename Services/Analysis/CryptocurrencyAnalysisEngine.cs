using FuturesSignalsBot.Indicators.Smoothing;
using FuturesSignalsBot.Models;

namespace FuturesSignalsBot.Services.Analysis;

public static class CryptocurrencyAnalysisEngine
{
    public static void InitializeIndicators(Cryptocurrency cryptocurrency)
    {
        var thirtyMinuteData = cryptocurrency.TradingDataContainer.ThirtyMinuteData;
        var lastItem30M = thirtyMinuteData.Last();
        
        CryptoAnalysisTools.CalculateAllTmoValues(thirtyMinuteData, cryptocurrency.Name is "BTCUSDT");
        lastItem30M.Tmo60 = CryptoAnalysisTools.CalculateLastTmoForTimeFrame(thirtyMinuteData,60);
        
        LsmaSmoothing.Smooth(cryptocurrency.TradingDataContainer.FourHourData);
        LsmaSmoothing.Smooth(cryptocurrency.TradingDataContainer.ThirtyMinuteData);
        LsmaSmoothing.Smooth(cryptocurrency.TradingDataContainer.FiveMinuteData);
    }

    public static void CalculateLastItemIndicators(Cryptocurrency cryptocurrency)
    {
        var thirtyMinuteData = cryptocurrency.TradingDataContainer.ThirtyMinuteData;
        var lastItem30M = thirtyMinuteData.Last();
        
        lastItem30M.Tmo30 = CryptoAnalysisTools.CalculateLastTmoForTimeFrame(thirtyMinuteData,30);
        lastItem30M.Tmo60 = CryptoAnalysisTools.CalculateLastTmoForTimeFrame(thirtyMinuteData,60);
        lastItem30M.Tmo180 = CryptoAnalysisTools.CalculateLastTmoForTimeFrame(thirtyMinuteData,180);
        
        if (cryptocurrency.Name is "BTCUSDT")
        {
            lastItem30M.Tmo240 = CryptoAnalysisTools.CalculateLastTmoForTimeFrame(thirtyMinuteData,240);
        }
    }
}