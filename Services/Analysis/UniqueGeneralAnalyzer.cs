using System.Text;
using FuturesSignalsBot.Core;
using FuturesSignalsBot.Enums;
using FuturesSignalsBot.Models.IndicatorResults;
using FuturesSignalsBot.Services.Notifiers;

namespace FuturesSignalsBot.Services.Analysis;

public static class UniqueGeneralAnalyzer
{
    private static string _report = string.Empty;

    public static List<PreliminaryImpulse> TopTmoX3Liquidation { get; private set; } = [];
    
    

    public static void AnalyzeDataList()
    {
        const string header = "<b>Список DATA</b>\n\n";
        _report = string.Empty;
        var sb = new StringBuilder();
            
        if (TmoIndexAnalyzer.OverSoldIndex < -8)
        {
            sb.AppendLine($"• Индекс ТМО: {TmoIndexAnalyzer.OverSoldIndex:F2} <b>LONG</b>\n");
        }
        else if (TmoIndexAnalyzer.OverSoldIndex > 8)
        {
            sb.AppendLine($"• Индекс ТМО: {TmoIndexAnalyzer.OverSoldIndex:F2} <b>SHORT</b>\n");
        }
        
        if (MarketAbsorptionAnalyzer.Absorption.AverageHigherPocPercentageChange > 4)
        {
            sb.AppendLine($"• Среднее %-изменение над POC: {MarketAbsorptionAnalyzer.Absorption.AverageHigherPocPercentageChange:F2}% <b>SHORT</b>");
        }

        if (MarketAbsorptionAnalyzer.Absorption.AverageLowerPocPercentageChange > 5)
        {
            sb.AppendLine($"• Среднее %-изменение под POC: {MarketAbsorptionAnalyzer.Absorption.AverageLowerPocPercentageChange:F2}% <b>LONG</b>");
        }
            
        if (sb.Length == 0)
        {
            _report = "Список DATA пуст";
            return;
        }
            
        _report = header + sb;
        _report += $"• Кол-во валют выше POC_0: {MarketAbsorptionAnalyzer.Absorption.HigherPocCount} / {MarketAbsorptionAnalyzer.Absorption.HigherPocPercentage:F2}%\n" +
                   $"• Кол-во валют ниже POC_0: {MarketAbsorptionAnalyzer.Absorption.LowerPocCount} / {MarketAbsorptionAnalyzer.Absorption.LowerPocPercentage:F2}%";
    }

    public static void AnalyzeImpulses(List<PreliminaryImpulse> impulses)
    {
        TopTmoX3Liquidation = impulses
            .Where(impulse => impulse.WasIntersection &&
                              ((impulse.IsLong && impulse is { IsMax: true, TmoX3: > 8 }) ||
                               impulse is { IsLong: false, IsMax: false, TmoX3: < -8 }) &&
                              impulse.LiquidationLevelNumber > 6)
            .ToList();
        
    }

    public static async Task SendReport()
    {
        if (_report.Equals("Список DATA пуст") && TopTmoX3Liquidation.Count == 0)
        {
            _report = "Списки пусты";
            await GlobalClients.TelegramBotService.SendMessageToSecondGroup(_report);
            return;
        }

        await LiquidationNotifier.SendTopLiquidationData(LiquidationLevelTopType.TmoX3Liquidation);
        await GlobalClients.TelegramBotService.SendMessageToSecondGroup(_report);
    }
}