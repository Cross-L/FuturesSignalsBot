using FuturesSignalsBot.Enums;

namespace FuturesSignalsBot.NameFactories;

public static class LiquidationLevelNameFactory
{
    public static string GetLiquidationLevelNameByType(LiquidationLevelTopType type)
    {
        return type switch
        {
            LiquidationLevelTopType.ShortInefficiency => "Неэффективность ТМО (+8.00)",
            LiquidationLevelTopType.LongInefficiency => "Неэффективность ТМО (-8.00)",
            LiquidationLevelTopType.HigherPoc => "Выше POC_0",
            LiquidationLevelTopType.LowerPoc => "Ниже POC_0",
            LiquidationLevelTopType.TmoX3Liquidation => "Ликвидация + ТМОX3",
            LiquidationLevelTopType.Low => "Toп_Лой",
            LiquidationLevelTopType.High => "Toп_Хай",
            LiquidationLevelTopType.LongLiquidation => "LONG📈Liqudations",
            LiquidationLevelTopType.ShortLiquidation => "SHORT📉Liqudations",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}