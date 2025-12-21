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
            LiquidationLevelTopType.LongLiquidation => "(LONG📈)Liqudations",
            LiquidationLevelTopType.ShortLiquidation => "(SHORT📉)Liqudations",
            LiquidationLevelTopType.ShortZScore => "(SHORT📉)Z-score",
            LiquidationLevelTopType.LongZScore => "(LONG📈)Z-score",
            LiquidationLevelTopType.ShortZScorePercentage => "(SHORT📉)Deliq Z",
            LiquidationLevelTopType.LongZScorePercentage => "(LONG📈)Deliq Z",
            LiquidationLevelTopType.LongOpenMax => "(LONG📈)OpenMax",
            LiquidationLevelTopType.ShortOpenMin => "(SHORT📉)OpenMin",
            LiquidationLevelTopType.BestLongs => "BEST_LONGS🔫",
            LiquidationLevelTopType.BestShorts => "BEST_SHORTS🔫",
            LiquidationLevelTopType.LongFundingRate => "📗F.rate_LONG🍭",
            LiquidationLevelTopType.ShortFundingRate => "📕F.rate_SHORT🍭",
            LiquidationLevelTopType.LongReverseNarrative => "✳️Reverse_narative_LONG🐳",
            LiquidationLevelTopType.ShortReverseNarrative => "✴️Reverse_narative_SHORT🐳",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}