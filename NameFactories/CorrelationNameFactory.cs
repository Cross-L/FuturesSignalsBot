using System;
using FuturesSignalsBot.Enums;

namespace FuturesSignalsBot.NameFactories;

public static class CorrelationNameFactory
{
    public static string GetCorrelationNameByType(CorrelationType type)
    {
        return type switch
        {
            CorrelationType.MaxExtremeCorrelation => "Наибольшая корреляция в рамках min/max BTC",
            CorrelationType.MinExtremeCorrelation => "Наименьшая корреляция в рамках min/max BTC",
            CorrelationType.AntiExtremeCorrelation => "Антикорреляция в рамках min/max BTC",
            CorrelationType.MaxSeriesCorrelation => "Наибольшая корреляция в рамках серии BTC",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}