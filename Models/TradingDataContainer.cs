using System.Collections.Generic;
using FuturesSignalsBot.Models.Resistance;

namespace FuturesSignalsBot.Models;

public class TradingDataContainer
{
    public List<CryptocurrencyDataItem> FiveMinuteData { get; set; } = [];
    public List<CryptocurrencyDataItem> FourHourData { get; set; } = [];
    public List<CryptocurrencyDataItem> ThirtyMinuteData { get; set; } = [];
    public List<BasicResistanceInfo> ResistanceInfos { get; } = [];
    
    public readonly LiquidationLevelContainer[] LongFibonacciLevels =
    [
        new(0.236m, 0m),
        new(0.382m, 0m),
        new(0.5m, 0m),
        new(0.618m, 0m),
        new(1.0m, 0m),
        new(1.5m, 0m),
        new(1.618m, 0m),
        new(1.786m, 0m),
        new(2.0m, 0m)
    ];
    
    public readonly LiquidationLevelContainer[] ShortFibonacciLevels =
    [
        new(0m, 0m),
        new(0.236m, 0m),
        new(0.382m,0m),
        new(0.5m, 0m),
        new(0.618m, 0m),
        new(0.786m, 0m),
    ];

    public void Clear()
    {
        FiveMinuteData.Clear();
        FourHourData.Clear();
        ResistanceInfos.Clear();
        ThirtyMinuteData.Clear();
    }
}