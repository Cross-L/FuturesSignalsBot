using FuturesSignalsBot.Core;
using FuturesSignalsBot.Models.IndicatorResults;
using FuturesSignalsBot.Services.Trading;

namespace FuturesSignalsBot.Services.Analysis;

public static class MarketAbsorptionAnalyzer
{
    public static List<PreliminaryImpulse> TopByHigherPoc { get; private set; } = [];
    public static List<PreliminaryImpulse> TopByLowerPoc { get; private set; } = [];
    
    public static List<string> HigherPocCurrencies { get; private set; } = [];
    public static List<string> LowerPocCurrencies { get; private set; } = [];
    public static MarketAbsorptionValues Absorption { get; private set; } = new();
    
    public static void AnalyzeLastAbsorption(List<CryptocurrencyManagementService> activeServices, List<PreliminaryImpulse?> preliminaryImpulses5M)
    {
        var totalCurrencies = activeServices.Count;
        var nonNullImpulses = preliminaryImpulses5M.OfType<PreliminaryImpulse>().ToList();
        var higherPocImpulses = nonNullImpulses.Where(impulse => impulse.PocPercentageChange > 0).ToList();
        var lowerPocImpulses = nonNullImpulses.Where(impulse => impulse.PocPercentageChange < 0).ToList();

        HigherPocCurrencies = higherPocImpulses.Select(impulse => impulse.Currency).ToList();
        LowerPocCurrencies = lowerPocImpulses.Select(impulse => impulse.Currency).ToList();

        double totalHigherPercentageChange = 0;
        double totalLowerPercentageChange = 0;
        var higherCount = 0;
        var lowerCount = 0;

        foreach (var currency in activeServices.Select(s => s.Cryptocurrency))
        {
            var tradingData = currency.TradingDataContainer.ThirtyMinuteData;
            var item = tradingData.Last();
            var poc = item.VolumeProfileData[0].SmoothedPoc;
            var percentageChange = (double)CryptoAnalysisTools.CalculatePositivePercentageChange(poc, item.Close);

            if (item.Close > poc)
            {
                totalHigherPercentageChange += percentageChange;
                higherCount++;
            }
            else
            {
                totalLowerPercentageChange += percentageChange;
                lowerCount++;
            }
        }
        
        var higherPocPercentage = totalCurrencies > 0 ? higherCount / (double)totalCurrencies * 100 : 0;
        var lowerPocPercentage = totalCurrencies > 0 ? lowerCount / (double)totalCurrencies * 100 : 0;
        
        var marketAbsorptionValues = new MarketAbsorptionValues
        {
            HigherPocCount = higherCount,
            LowerPocCount = lowerCount,
            HigherPocPercentage = higherPocPercentage,
            LowerPocPercentage = lowerPocPercentage,
            AverageHigherPocPercentageChange = higherCount > 0 ? totalHigherPercentageChange / higherCount : 0,
            AverageLowerPocPercentageChange = lowerCount > 0 ? totalLowerPercentageChange / lowerCount : 0
        };

        Absorption = marketAbsorptionValues;
        
        higherPocImpulses = higherPocImpulses.Where(impulse =>
        {
            if (impulse.WasIntersection)
            {
                return HigherPocCurrencies.Contains(impulse.Currency) && impulse is { IsLong: true, PocPercentageChange: > 0 };
            }

            return HigherPocCurrencies.Contains(impulse.Currency);
        }).ToList();

        lowerPocImpulses = lowerPocImpulses.Where(impulse =>
        {
            if (impulse.WasIntersection)
            {
                return LowerPocCurrencies.Contains(impulse.Currency) && impulse is { IsLong: false, PocPercentageChange: < 0 };
            }

            return LowerPocCurrencies.Contains(impulse.Currency);
        }).ToList();


        TopByHigherPoc = higherPocImpulses.OrderByDescending(impulse => impulse.PocPercentageChange).ToList();
        if (TopByHigherPoc.Any(impulse => impulse.Currency == "BTCUSDT"))
        {
            var btcImpulse = TopByHigherPoc.First(impulse => impulse.Currency == "BTCUSDT");
            TopByHigherPoc.Remove(btcImpulse);
            TopByHigherPoc.Insert(0, btcImpulse);
        }
        TopByHigherPoc = TopByHigherPoc.Take(20).ToList();

        TopByLowerPoc = lowerPocImpulses.OrderBy(impulse => impulse.PocPercentageChange).ToList();
        if (TopByLowerPoc.Any(impulse => impulse.Currency == "BTCUSDT"))
        {
            var btcImpulse = TopByLowerPoc.First(impulse => impulse.Currency == "BTCUSDT");
            TopByLowerPoc.Remove(btcImpulse);
            TopByLowerPoc.Insert(0, btcImpulse);
        }
        TopByLowerPoc = TopByLowerPoc.Take(20).ToList();
    }

    public static async Task SendAbsorptionReportAsync(long chatId)
    {
        var report = $"🧮 Поглощенные продажи/покупки 🧮\n\n" +
                     $"• <b>Среднее %-изменение над POC</b>: {Absorption.AverageHigherPocPercentageChange:F2}%\n" +
                     $"• <b>Среднее %-изменение под POC</b>: {Absorption.AverageLowerPocPercentageChange:F2}%\n" +
                     $"• <b>Кол-во валют выше POC_0</b>: {Absorption.HigherPocCount} / {Absorption.HigherPocPercentage:F2}%\n" +
                     $"• <b>Кол-во валют ниже POC_0</b>: {Absorption.LowerPocCount} / {Absorption.LowerPocPercentage:F2}%";

        await GlobalClients.TelegramBotService.SendMessageToChatAsync(chatId, report);
    }
}