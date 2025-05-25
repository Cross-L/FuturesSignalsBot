using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        LiquidationLevelTopType.TmoX3Liquidation,
    ];
    
    
    private static readonly HashSet<LiquidationLevelTopType> ChangeOvTopTypes =
    [
        LiquidationLevelTopType.Low,
        LiquidationLevelTopType.High
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
            _ => throw new ArgumentOutOfRangeException(nameof(topListType), topListType, null)
        };

        var message = impulses.Count == 0 ? $"Список {header} пуст" : BuildLiquidationMessage();

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

        return;

        string BuildLiquidationMessage()
        {
            var messageBuilder = new StringBuilder();
            AppendLiquidationLevelData(messageBuilder, header, impulses, topListType);
            return messageBuilder.ToString();
        }
    }

    private static void AppendLiquidationLevelData(StringBuilder stringBuilder, string header,
        IReadOnlyCollection<PreliminaryImpulse> impulses, LiquidationLevelTopType topListType)
    {
        stringBuilder.AppendLine($"<b>{header}</b>\n");

        var btcImpulse = impulses.FirstOrDefault(impulse => impulse.Currency == "BTCUSDT");
        if (btcImpulse != null)
        {
            stringBuilder.AppendLine("<b>🔸 Биткоин:</b>");
            AppendPreliminaryImpulse(stringBuilder, btcImpulse, 0, topListType);
            stringBuilder.AppendLine();
        }

        var altCoins = impulses.Where(impulse => impulse.Currency != "BTCUSDT").ToList();
        
        if (altCoins.Count != 0)
        {
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

                
                stringBuilder.AppendLine("<b>🔹 Альткоины(Lo_imp):</b>");
                for (var i = 0; i < longImpulses.Count; i++)
                {
                    AppendPreliminaryImpulse(stringBuilder, longImpulses[i], i + 1, topListType);
                }
                
                stringBuilder.AppendLine("<b>🔹 Альткоины(Sh_imp):</b>");
                for (var i = 0; i < shortImpulses.Count; i++)
                {
                    AppendPreliminaryImpulse(stringBuilder, shortImpulses[i], i + 1, topListType);
                }
            }
            else
            {
                stringBuilder.AppendLine("<b>🔹 Альткоины:</b>");
                for (var i = 0; i < altCoins.Count; i++)
                {
                    AppendPreliminaryImpulse(stringBuilder, altCoins[i], i + 1, topListType);
                }
            }
        }
        
    }


    private static void AppendPreliminaryImpulse(StringBuilder liquidationData,
        PreliminaryImpulse preliminaryImpulse, int number, LiquidationLevelTopType topListType)
    {
        var impulseType = preliminaryImpulse.IsLong ? "📈 Lo_Imp" : "📉 Sh_Imp";
        var precision = Math.Min(preliminaryImpulse.Precision, 7);
        var isAltCoin = number != 0;

        string formattedLine;
        var isChangeOvType = ChangeOvTopTypes.Contains(topListType);
        
        var sign = preliminaryImpulse.PocPercentageChange < 0 ? "" : "+";
        
        var poc0Text = $" [poc{sign}{preliminaryImpulse.PocPercentageChange:F2}%]";
        var changeOvText = isChangeOvType ? $", changeOv[<b>{preliminaryImpulse.ChangeOv:F2}%</b>]" : "";
        
        if (isAltCoin)
        {
            formattedLine = topListType is LiquidationLevelTopType.LongInefficiency or LiquidationLevelTopType.ShortInefficiency 
                ? $"<b>{number}. {preliminaryImpulse.Currency}.P{poc0Text}</b>." 
                : $"<b>{number}. {preliminaryImpulse.Currency}.P{poc0Text}{changeOvText} ({impulseType})</b>\n";
        }
        else
        {
            formattedLine = $"<b>{number}. {preliminaryImpulse.Currency}.P{poc0Text}{changeOvText} ({impulseType})</b>\n";
        }
        
        var isSpecialType = OverSoldIndexTopTypes.Contains(topListType);
        
        var baseLine = preliminaryImpulse.WasIntersection
            ? $"LastLiq {preliminaryImpulse.LiquidationLevel!} " +
              $"({preliminaryImpulse.IntersectionItem!.CloseTime.AddHours(2):dd.MM HH:mm:ss} " +
              $"Цена: <b>{preliminaryImpulse.Price.ToString($"F{precision}")}</b>)."
            : string.Empty;
        
        var tmoPart = isSpecialType
            ? $"<b>ТМОх3({preliminaryImpulse.TmoX3:F1})</b> "
            : $"<b>ТМО({preliminaryImpulse.Tmo30:F1})</b> ";
        
        formattedLine += baseLine + tmoPart;
        liquidationData.AppendLine(formattedLine);
    }
}