using System.Text;
using FuturesSignalsBot.Core;
using FuturesSignalsBot.Enums;
using FuturesSignalsBot.Models.IndicatorResults;
using FuturesSignalsBot.NameFactories;
using FuturesSignalsBot.Services.Analysis;

namespace FuturesSignalsBot.Services.Notifiers;

public static class LiquidationNotifier
{
    private static readonly HashSet<LiquidationLevelTopType> OverSoldIndexTopTypes =
    [
        LiquidationLevelTopType.LongInefficiency,
        LiquidationLevelTopType.ShortInefficiency,
        LiquidationLevelTopType.HigherPoc,
        LiquidationLevelTopType.LowerPoc,
        LiquidationLevelTopType.TmoX3Liquidation
    ];

    private static readonly HashSet<LiquidationLevelTopType> ChangeOvTopTypes =
    [
        LiquidationLevelTopType.Low,
        LiquidationLevelTopType.High
    ];

    private static readonly HashSet<LiquidationLevelTopType> OnlyLiquidationTypes =
    [
        LiquidationLevelTopType.LongLiquidation,
        LiquidationLevelTopType.ShortLiquidation
    ];

    public static async Task SendTopLiquidationData(LiquidationLevelTopType topListType, long chatId = 0)
    {
        var header = LiquidationLevelNameFactory.GetLiquidationLevelNameByType(topListType);

        var impulses = topListType switch
        {
            LiquidationLevelTopType.LongInefficiency => TmoIndexAnalyzer.TopByLongInefficiency,
            LiquidationLevelTopType.ShortInefficiency => TmoIndexAnalyzer.TopByShortInefficiency,
            LiquidationLevelTopType.HigherPoc => MarketAbsorptionAnalyzer.TopByHigherPoc,
            LiquidationLevelTopType.LowerPoc => MarketAbsorptionAnalyzer.TopByLowerPoc,
            LiquidationLevelTopType.TmoX3Liquidation => UniqueGeneralAnalyzer.TopTmoX3Liquidation,
            LiquidationLevelTopType.Low => PreliminaryImpulseAnalyzer.TopByLow,
            LiquidationLevelTopType.High => PreliminaryImpulseAnalyzer.TopByHigh,
            LiquidationLevelTopType.LongLiquidation => PreliminaryImpulseAnalyzer.TopLongLiquidations,
            LiquidationLevelTopType.ShortLiquidation => PreliminaryImpulseAnalyzer.TopShortLiquidations,
            _ => throw new ArgumentOutOfRangeException(nameof(topListType), topListType, null)
        };

        var message = impulses.Count == 0
            ? $"⚠️ Список <b>{header}</b> пуст"
            : BuildLiquidationMessage(new StringBuilder(), header, impulses, topListType);

        if (topListType == LiquidationLevelTopType.TmoX3Liquidation)
        {
            await GlobalClients.TelegramBotService.SendMessageToSecondGroup(message);
            return;
        }

        if (chatId == 0)
        {
            await GlobalClients.TelegramBotService.SendMessageToGroup(message);
        }
        else
        {
            await GlobalClients.TelegramBotService.SendMessageToChatAsync(chatId, message);
        }
    }

    private static string BuildLiquidationMessage(StringBuilder stringBuilder, string header,
        IReadOnlyCollection<PreliminaryImpulse> impulses, LiquidationLevelTopType topListType)
    {
        stringBuilder.Clear();
        AppendLiquidationLevelData(stringBuilder, header, impulses, topListType);
        return stringBuilder.ToString();
    }

    private static void AppendLiquidationLevelData(StringBuilder stringBuilder, string header,
        IReadOnlyCollection<PreliminaryImpulse> impulses, LiquidationLevelTopType topListType)
    {
        stringBuilder.AppendLine($"📊 <b>{header}</b>\n");

        var btcImpulse = impulses.FirstOrDefault(impulse => impulse.Currency == "BTCUSDT");
        if (btcImpulse != null)
        {
            stringBuilder.AppendLine("🔸 <b>Биткоин:</b>");
            AppendPreliminaryImpulse(stringBuilder, btcImpulse, 0, topListType);
            stringBuilder.AppendLine();
        }

        var altCoins = impulses.Where(impulse => impulse.Currency != "BTCUSDT").ToList();
        if (altCoins.Count == 0) return;

        if (topListType == LiquidationLevelTopType.TmoX3Liquidation)
        {
            var longImpulses = altCoins
                .Where(impulse => impulse.IsLong)
                .OrderByDescending(impulse => impulse.LiquidationLevelNumber)
                .Take(20)
                .ToList();

            var shortImpulses = altCoins
                .Where(impulse => !impulse.IsLong)
                .OrderByDescending(impulse => impulse.LiquidationLevelNumber)
                .Take(20)
                .ToList();

            stringBuilder.AppendLine("🔹 <b>Альткоины (Lo_Imp):</b>");
            for (var i = 0; i < longImpulses.Count; i++)
            {
                AppendPreliminaryImpulse(stringBuilder, longImpulses[i], i + 1, topListType);
            }

            stringBuilder.AppendLine("\n🔹 <b>Альткоины (Sh_Imp):</b>");
            for (var i = 0; i < shortImpulses.Count; i++)
            {
                AppendPreliminaryImpulse(stringBuilder, shortImpulses[i], i + 1, topListType);
            }
        }
        else
        {
            stringBuilder.AppendLine("🔹 <b>Альткоины:</b>");
            for (var i = 0; i < altCoins.Count; i++)
            {
                AppendPreliminaryImpulse(stringBuilder, altCoins[i], i + 1, topListType);
            }
        }
    }

    private static void AppendPreliminaryImpulse(StringBuilder liquidationData,
        PreliminaryImpulse preliminaryImpulse, int number, LiquidationLevelTopType topListType)
    {
        var precision = Math.Min(preliminaryImpulse.Precision, 7);
        var isAltCoin = number != 0;
        string formattedLine;

        if (OnlyLiquidationTypes.Contains(topListType))
        {
            var symbol = !preliminaryImpulse.IsLong ? "💧" : "🩸";
            formattedLine = isAltCoin
                ? $"{number}. <b>{preliminaryImpulse.Currency}</b>.P {symbol}{preliminaryImpulse.LiquidationLevel} [price:{preliminaryImpulse.Price.ToString($"F{precision}")}]"
                : $"<b>{preliminaryImpulse.Currency}</b>.P {symbol}{preliminaryImpulse.LiquidationLevel} [price:{preliminaryImpulse.Price.ToString($"F{precision}")}]";

            liquidationData.AppendLine(formattedLine);
            return;
        }

        var impulseType = preliminaryImpulse.IsLong ? "📈 Lo_Imp" : "📉 Sh_Imp";
        var isChangeOvType = ChangeOvTopTypes.Contains(topListType);
        var sign = preliminaryImpulse.PocPercentageChange < 0 ? "" : "+";
        var pocText = $"[poc{sign}{preliminaryImpulse.PocPercentageChange:F2}%]";
        var changeOvText = isChangeOvType ? $", changeOv [<b>{preliminaryImpulse.ChangeOv:F2}%</b>]" : "";

        formattedLine = isAltCoin &&
                            topListType is LiquidationLevelTopType.LongInefficiency
                                or LiquidationLevelTopType.ShortInefficiency
            ? $"{number}. <b>{preliminaryImpulse.Currency}</b>.P{pocText}"
            : $"{number}. <b>{preliminaryImpulse.Currency}</b>.P{pocText}{changeOvText} ({impulseType})";

        var isSpecialType = OverSoldIndexTopTypes.Contains(topListType);
        var tmoPart = isSpecialType
            ? $"<b>ТМОх3({preliminaryImpulse.TmoX3:F1})</b> "
            : $"<b>ТМО({preliminaryImpulse.Tmo30:F1})</b> ";

        var baseLine = preliminaryImpulse.WasIntersection
            ? $"LastLiq {preliminaryImpulse.LiquidationLevel} " +
              $"({preliminaryImpulse.IntersectionItem!.CloseTime.AddHours(2):dd.MM HH:mm:ss} " +
              $"Цена: <b>{preliminaryImpulse.Price.ToString($"F{precision}")}$</b>)"
            : "";

        liquidationData.AppendLine($"{formattedLine} {baseLine} {tmoPart}");
    }
}