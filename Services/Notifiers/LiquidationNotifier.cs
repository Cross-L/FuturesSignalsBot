using System.Text;
using FuturesSignalsBot.Core;
using FuturesSignalsBot.Enums;
using FuturesSignalsBot.Models.IndicatorResults;
using FuturesSignalsBot.NameFactories;
using FuturesSignalsBot.Services.Analysis;

namespace FuturesSignalsBot.Services.Notifiers;

public static class LiquidationNotifier
{
    private static readonly Dictionary<LiquidationLevelTopType, (Func<IReadOnlyCollection<PreliminaryImpulse>> Analyzer, string FormatType)> TypeConfig = new()
    {
        { LiquidationLevelTopType.LongInefficiency, (() => TmoIndexAnalyzer.TopByLongInefficiency, "Inefficiency") },
        { LiquidationLevelTopType.ShortInefficiency, (() => TmoIndexAnalyzer.TopByShortInefficiency, "Inefficiency") },
        { LiquidationLevelTopType.HigherPoc, (() => MarketAbsorptionAnalyzer.TopByHigherPoc, "Poc") },
        { LiquidationLevelTopType.LowerPoc, (() => MarketAbsorptionAnalyzer.TopByLowerPoc, "Poc") },
        { LiquidationLevelTopType.TmoX3Liquidation, (() => UniqueGeneralAnalyzer.TopTmoX3Liquidation, "TmoX3") },
        { LiquidationLevelTopType.Low, (() => PreliminaryImpulseAnalyzer.TopByLow, "ChangeOv") },
        { LiquidationLevelTopType.High, (() => PreliminaryImpulseAnalyzer.TopByHigh, "ChangeOv") },
        { LiquidationLevelTopType.LongLiquidation, (() => PreliminaryImpulseAnalyzer.TopLongLiquidations, "Liquidation") },
        { LiquidationLevelTopType.ShortLiquidation, (() => PreliminaryImpulseAnalyzer.TopShortLiquidations, "Liquidation") },
        { LiquidationLevelTopType.LongZScore, (() => PreliminaryImpulseAnalyzer.TopLongZScore, "ZScore") },
        { LiquidationLevelTopType.ShortZScore, (() => PreliminaryImpulseAnalyzer.TopShortZScore, "ZScore") },
        { LiquidationLevelTopType.LongZScorePercentage, (() => PreliminaryImpulseAnalyzer.TopLongZScorePercentage, "ZScorePercentage") },
        { LiquidationLevelTopType.ShortZScorePercentage, (() => PreliminaryImpulseAnalyzer.TopShortZScorePercentage, "ZScorePercentage") },
        { LiquidationLevelTopType.LongOpenMax, (() => PreliminaryImpulseAnalyzer.TopLongOpenMax, "OpenMinMax") },
        { LiquidationLevelTopType.ShortOpenMin, (() => PreliminaryImpulseAnalyzer.TopShortOpenMin, "OpenMinMax") },
        { LiquidationLevelTopType.BestLongs, (() => PreliminaryImpulseAnalyzer.BestLongs, "BestImpulses") },
        { LiquidationLevelTopType.BestShorts, (() => PreliminaryImpulseAnalyzer.BestShorts, "BestImpulses") }
    };

    public static async Task SendTopLiquidationData(LiquidationLevelTopType topListType, long chatId = 0)
    {
        if (!TypeConfig.TryGetValue(topListType, out var config))
            throw new ArgumentOutOfRangeException(nameof(topListType), topListType, null);

        var header = LiquidationLevelNameFactory.GetLiquidationLevelNameByType(topListType);
        var impulses = config.Analyzer();

        var message = impulses.Count == 0
            ? $"⚠️ Список <b>{header}</b> пуст"
            : BuildLiquidationMessage(header, impulses, topListType, config.FormatType);

        if (topListType == LiquidationLevelTopType.TmoX3Liquidation)
        {
            await GlobalClients.TelegramBotService.SendMessageToSecondGroup(message);
            return;
        }

        if (chatId == 0)
            await GlobalClients.TelegramBotService.SendMessageToGroup(message);
        else
            await GlobalClients.TelegramBotService.SendMessageToChatAsync(chatId, message);
    }

    private static string BuildLiquidationMessage(string header, IReadOnlyCollection<PreliminaryImpulse> impulses, 
        LiquidationLevelTopType topListType, string formatType)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"📊 <b>{header}</b>\n");

        var btcImpulse = impulses.FirstOrDefault(impulse => impulse.Currency == "BTCUSDT");
        if (btcImpulse != null)
        {
            sb.AppendLine("🔸 <b>Биткоин:</b>");
            AppendPreliminaryImpulse(sb, btcImpulse, 0, topListType, formatType);
            sb.AppendLine();
        }

        var altCoins = impulses.Where(impulse => impulse.Currency != "BTCUSDT").ToList();
        if (altCoins.Count == 0) return sb.ToString();

        if (topListType == LiquidationLevelTopType.TmoX3Liquidation)
        {
            var longImpulses = altCoins.Where(impulse => impulse.IsLong)
                .OrderByDescending(impulse => impulse.LiquidationLevelNumber)
                .Take(20)
                .ToList();

            var shortImpulses = altCoins.Where(impulse => !impulse.IsLong)
                .OrderByDescending(impulse => impulse.LiquidationLevelNumber)
                .Take(20)
                .ToList();

            sb.AppendLine("🔹 <b>Альткоины (Lo_Imp):</b>");
            for (var i = 0; i < longImpulses.Count; i++)
                AppendPreliminaryImpulse(sb, longImpulses[i], i + 1, topListType, formatType);

            sb.AppendLine("\n🔹 <b>Альткоины (Sh_Imp):</b>");
            for (var i = 0; i < shortImpulses.Count; i++)
                AppendPreliminaryImpulse(sb, shortImpulses[i], i + 1, topListType, formatType);
        }
        else
        {
            sb.AppendLine("🔹 <b>Альткоины:</b>");
            for (var i = 0; i < altCoins.Count; i++)
                AppendPreliminaryImpulse(sb, altCoins[i], i + 1, topListType, formatType);
        }

        return sb.ToString();
    }

    private static void AppendPreliminaryImpulse(StringBuilder sb, PreliminaryImpulse impulse, int number, 
        LiquidationLevelTopType topListType, string formatType)
    {
        var precision = Math.Min(impulse.Precision, 7);
        var isAltCoin = number != 0;
        var icon = impulse.IsLong ? "🩸" : "💧";
        var price = impulse.Price.ToString($"F{precision}");
        var baseFormat = isAltCoin 
            ? $"{number}. <b>{impulse.Currency}</b>.P" 
            : $"<b>{impulse.Currency}</b>.P";

        switch (formatType)
        {
            case "Liquidation":
                sb.AppendLine($"{baseFormat} {icon}{impulse.LiquidationLevel} [price:{price}]");
                break;

            case "ZScore":
                var zScore = impulse.IsLong ? impulse.Score.ZScore : impulse.Score.InvertedZScore;
                var zScoreLine = impulse.LiquidationLevel != null
                    ? $"{baseFormat} 💢Zscr [{zScore:F5}] {icon}{impulse.LiquidationLevel}"
                    : $"{baseFormat} 💢Zscr [{zScore:F5}]";
                sb.AppendLine(zScoreLine);
                break;

            case "ZScorePercentage":
                zScore = impulse.IsLong ? impulse.Score.ZScore : impulse.Score.InvertedZScore;
                var zPercentageIcon = impulse.IsLong ? "🔋" : "🪫";
                var zScorePercentageLine = impulse.LiquidationLevel != null
                    ? $"{baseFormat} {icon}{impulse.LiquidationLevel} 💢Zscr [{zScore:F5}], {zPercentageIcon}Del.Z[{impulse.ZScoreRatio:F1}%]"
                    : $"{baseFormat} 💢Zscr [{zScore:F5}], {zPercentageIcon}Del.Z[{impulse.ZScoreRatio:F1}%]";
                sb.AppendLine(zScorePercentageLine);
                break;

            case "OpenMinMax":
                zScore = impulse.IsLong ? impulse.Score.ZScore : impulse.Score.InvertedZScore;
                zPercentageIcon = impulse.IsLong ? "🔋" : "🪫";
                var minMaxInfo = impulse.IsLong ? "🍎Op.max" : "🍏Op.min";
                var openMinMaxLine = impulse.LiquidationLevel != null
                    ? $"{baseFormat} {icon}{impulse.LiquidationLevel} 💢Zscr [{zScore:F5}], {zPercentageIcon}Del.Z[{impulse.ZScoreRatio:F1}%], {minMaxInfo}[{impulse.MinMaxPercentage:F1}%]"
                    : $"{baseFormat} 💢Zscr [{zScore:F5}], {zPercentageIcon}Del.Z[{impulse.ZScoreRatio:F1}%], {minMaxInfo}[{impulse.MinMaxPercentage:F1}%]";
                sb.AppendLine(openMinMaxLine);
                break;

            case "BestImpulses":
                zScore = impulse.Score.ZScore;
                zPercentageIcon = impulse.IsLong ? "🔋" : "🪫";
                minMaxInfo = !impulse.IsLong ? "🍎Op.max" : "🍏Op.min";
                var bestImpulsesLine = $"{baseFormat}(⚡️{impulse.AverageZPercentage:F1}) - {icon}{impulse.LiquidationLevel} 💢Zscr [{zScore:F5}], {zPercentageIcon}Del.Z[{impulse.ZScoreRatio:F1}%], {minMaxInfo}[{impulse.MinMaxPercentage:F1}%], changeOv[{impulse.ChangeOv:F2}%]";
                sb.AppendLine(bestImpulsesLine);
                break;

            default: // Inefficiency, Poc, ChangeOv, TmoX3
                var impulseType = impulse.IsLong ? "📈 Lo_Imp" : "📉 Sh_Imp";
                var isChangeOvType = formatType == "ChangeOv";
                var sign = impulse.PocPercentageChange < 0 ? "" : "+";
                var pocText = $"[poc{sign}{impulse.PocPercentageChange:F2}%]";
                var changeOvText = isChangeOvType ? $", changeOv [<b>{impulse.ChangeOv:F2}%</b>]" : "";
                var formattedLine = isAltCoin && (topListType is LiquidationLevelTopType.LongInefficiency or LiquidationLevelTopType.ShortInefficiency)
                    ? $"{number}. <b>{impulse.Currency}</b>.P{pocText}"
                    : $"{number}. <b>{impulse.Currency}</b>.P{pocText}{changeOvText} ({impulseType})";

                var isSpecialType = formatType is "Inefficiency" or "Poc" or "TmoX3";
                var tmoPart = isSpecialType
                    ? $"<b>ТМОх3({impulse.TmoX3:F1})</b> "
                    : $"<b>ТМО({impulse.Tmo30:F1})</b> ";

                var baseLine = impulse.WasIntersection
                    ? $"LastLiq {impulse.LiquidationLevel} ({impulse.IntersectionItem!.CloseTime.AddHours(2):dd.MM HH:mm:ss} Цена: <b>{impulse.Price.ToString($"F{precision}")}$</b>)"
                    : "";

                sb.AppendLine($"{formattedLine} {baseLine} {tmoPart}");
                break;
        }
    }
}