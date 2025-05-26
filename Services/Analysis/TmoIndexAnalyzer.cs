using System.Collections.Concurrent;
using FuturesSignalsBot.Core;
using FuturesSignalsBot.Models.IndicatorResults;
using FuturesSignalsBot.Services.Trading;

namespace FuturesSignalsBot.Services.Analysis;

public static class TmoIndexAnalyzer
{
    private static double _sumTmo30;
    private static double _sumTmo60;
    private static double _sumTmo180;

    private static double _btcTmo30;
    private static double _btcTmo60;
    private static double _btcTmo180;
    private static double _btcOverSoldIndex;

    private static int _count;
    private static double _shortTerm;
    private static double _midTerm;
    private static double _longTerm;
    public static double OverSoldIndex { get; private set; }
    private static int _positiveInefficiencyCount;
    private static double _positiveInefficiencyPercentage;
    private static int _negativeInefficiencyCount;
    private static double _negativeInefficiencyPercentage;

    private static readonly object LockObj = new();

    public static List<PreliminaryImpulse> TopByShortInefficiency { get; private set; } = [];
    public static List<PreliminaryImpulse> TopByLongInefficiency { get; private set; } = [];
    
    public static void AnalyzeTmoIndicators(List<CryptocurrencyTradingService> activeServices)
    {
        _sumTmo30 = 0;
        _sumTmo60 = 0;
        _sumTmo180 = 0;
        _count = 0;

        var allCurrencies = activeServices.Select(service => service.Cryptocurrency).ToList();

        Parallel.ForEach(allCurrencies, currency =>
        {
            lock (LockObj)
            {
                var lastItem30M = currency.TradingDataContainer.ThirtyMinuteData.Last();
                _sumTmo30 += lastItem30M.Tmo30;
                _sumTmo60 += lastItem30M.Tmo60;
                _sumTmo180 += lastItem30M.Tmo180;
                _count++;
                currency.OversoldIndex = (lastItem30M.Tmo30 + lastItem30M.Tmo60 + lastItem30M.Tmo180) / 3;

                if (currency.Name.Equals("BTCUSDT", StringComparison.OrdinalIgnoreCase))
                {
                    _btcTmo30 = lastItem30M.Tmo30;
                    _btcTmo60 = lastItem30M.Tmo60;
                    _btcTmo180 = lastItem30M.Tmo180;
                    _btcOverSoldIndex = (lastItem30M.Tmo30 + lastItem30M.Tmo60 + lastItem30M.Tmo180) / 3;
                }
            }
        });

        if (_count > 0)
        {
            _shortTerm = _sumTmo30 / _count;
            _midTerm = _sumTmo60 / _count;
            _longTerm = _sumTmo180 / _count;
            OverSoldIndex = (_shortTerm + _midTerm + _longTerm) / 3;
        }
        else
        {
            _shortTerm = _midTerm = _longTerm = OverSoldIndex = 0;
        }
    }


    public static void AnalyzeTmoInefficiency(List<CryptocurrencyTradingService> activeServices, 
        List<PreliminaryImpulse?> preliminaryImpulses30M, List<PreliminaryImpulse?> preliminaryImpulses5M,
        List<PreliminaryImpulse?> specifiedPreliminaryImpulses)
    {
        var allCurrencies = activeServices.Select(service => service.Cryptocurrency).ToList();
        var highTmoCurrencies = new ConcurrentBag<string>();
        var lowTmoCurrencies = new ConcurrentBag<string>();

        var highTmoCurrencyCount = 0;
        var lowTmoCurrencyCount = 0;

        Parallel.ForEach(allCurrencies, currency =>
        {
            var thirtyMinuteData = currency.TradingDataContainer.ThirtyMinuteData;

            var highTmoSeriesCount = 0;
            var highTmoCandleCount = 0;
            var lowTmoSeriesCount = 0;
            var lowTmoCandleCount = 0;

            var currentHighTmoSeriesLength = 0;
            var currentLowTmoSeriesLength = 0;

            foreach (var item in thirtyMinuteData)
            {
                if (item.Tmo180 == 0)
                    continue;

                if (item.Tmo180 > 8.00)
                {
                    currentHighTmoSeriesLength++;
                }
                else
                {
                    if (currentHighTmoSeriesLength >= 2)
                    {
                        highTmoSeriesCount++;
                        highTmoCandleCount += currentHighTmoSeriesLength;
                    }

                    currentHighTmoSeriesLength = 0;
                }

                if (item.Tmo180 < -8.00)
                {
                    currentLowTmoSeriesLength++;
                }
                else
                {
                    if (currentLowTmoSeriesLength >= 2)
                    {
                        lowTmoSeriesCount++;
                        lowTmoCandleCount += currentLowTmoSeriesLength;
                    }

                    currentLowTmoSeriesLength = 0;
                }
            }

            if (currentHighTmoSeriesLength >= 2)
            {
                highTmoSeriesCount++;
                highTmoCandleCount += currentHighTmoSeriesLength;
            }

            if (currentLowTmoSeriesLength >= 2)
            {
                lowTmoSeriesCount++;
                lowTmoCandleCount += currentLowTmoSeriesLength;
            }

            var avgCandlesPerHighTmoSeries =
                highTmoSeriesCount > 0 ? (double)highTmoCandleCount / highTmoSeriesCount : 0;

            var avgCandlesPerLowTmoSeries =
                lowTmoSeriesCount > 0 ? (double)lowTmoCandleCount / lowTmoSeriesCount : 0;

            var lastCandle = thirtyMinuteData.Last();

            if (lastCandle.Tmo180 > 8.00 && currentHighTmoSeriesLength > avgCandlesPerHighTmoSeries)
            {
                highTmoCurrencies.Add(currency.Name);
                Interlocked.Increment(ref highTmoCurrencyCount);
            }

            if (lastCandle.Tmo180 < -8.00 && currentLowTmoSeriesLength > avgCandlesPerLowTmoSeries)
            {
                lowTmoCurrencies.Add(currency.Name);
                Interlocked.Increment(ref lowTmoCurrencyCount);
            }
        });

        var totalCurrencies = allCurrencies.Count;
        var highTmoPercentage = (double)highTmoCurrencyCount / totalCurrencies * 100;
        var lowTmoPercentage = (double)lowTmoCurrencyCount / totalCurrencies * 100;

        _positiveInefficiencyCount = highTmoCurrencyCount;
        _positiveInefficiencyPercentage = highTmoPercentage;
        _negativeInefficiencyCount = lowTmoCurrencyCount;
        _negativeInefficiencyPercentage = lowTmoPercentage;

        var currencyDictionary = GlobalClients.CryptocurrenciesStorage.AllCryptocurrencies
            .ToDictionary(currency => currency.Name, currency => currency.OversoldIndex);

        foreach (var impulse in preliminaryImpulses30M.Concat(specifiedPreliminaryImpulses)
                     .Concat(preliminaryImpulses5M)
                     .Where(impulse => impulse != null))
        {
            if (currencyDictionary.TryGetValue(impulse!.Currency, out var oversoldIndex))
            {
                impulse.TmoX3 = oversoldIndex;
            }
        }

        var nonNullImpulses = preliminaryImpulses30M.OfType<PreliminaryImpulse>().ToList();
        var allTmoCurrencyImpulses = nonNullImpulses.Where
                (impulse => lowTmoCurrencies.Contains(impulse.Currency) || highTmoCurrencies.Contains(impulse.Currency))
            .ToList();

        TopByLongInefficiency = allTmoCurrencyImpulses
            .OrderBy(impulse =>
                MarketAbsorptionAnalyzer.LowerPocCurrencies.Contains(impulse.Currency)
                    ? impulse.PocPercentageChange
                    : decimal.MaxValue)
            .ThenByDescending(impulse =>
                MarketAbsorptionAnalyzer.HigherPocCurrencies.Contains(impulse.Currency)
                    ? impulse.PocPercentageChange
                    : decimal.MinValue)
            .ToList();

        TopByShortInefficiency = allTmoCurrencyImpulses
            .OrderByDescending(impulse =>
                MarketAbsorptionAnalyzer.HigherPocCurrencies.Contains(impulse.Currency)
                    ? impulse.PocPercentageChange
                    : decimal.MinValue)
            .ThenBy(impulse =>
                MarketAbsorptionAnalyzer.LowerPocCurrencies.Contains(impulse.Currency)
                    ? impulse.PocPercentageChange
                    : decimal.MaxValue)
            .ToList();

        if (TopByLongInefficiency.Any(impulse => impulse.Currency == "BTCUSDT"))
        {
            var btcImpulse = TopByLongInefficiency.First(impulse => impulse.Currency == "BTCUSDT");
            TopByLongInefficiency.Remove(btcImpulse);
            TopByLongInefficiency.Insert(0, btcImpulse);
        }

        TopByLongInefficiency = TopByLongInefficiency.Take(20).ToList();

        if (TopByShortInefficiency.Any(impulse => impulse.Currency == "BTCUSDT"))
        {
            var btcImpulse = TopByShortInefficiency.First(impulse => impulse.Currency == "BTCUSDT");
            TopByShortInefficiency.Remove(btcImpulse);
            TopByShortInefficiency.Insert(0, btcImpulse);
        }

        TopByShortInefficiency = TopByShortInefficiency.Take(20).ToList();
    }

    public static async Task SendTmoIndexReportAsync(long chatId)
    {
        var report = $"📐 <b>Общий отчет по TMO</b> 📐\n\n" +
                     $"• <b>Индекс перекупленности/перепроданности</b>: {OverSoldIndex:F2}\n" +
                     $"• <b>Краткосрочный TMO</b>: {_shortTerm:F2}\n" +
                     $"• <b>Среднесрочный TMO</b>: {_midTerm:F2}\n" +
                     $"• <b>Долгосрочный TMO</b>: {_longTerm:F2}\n" +
                     $"• <b>Неэффективность TMO (+8.00)</b>: {_positiveInefficiencyCount} или {_positiveInefficiencyPercentage:F2}%\n" +
                     $"• <b>Неэффективность TMO (-8.00)</b>: {_negativeInefficiencyCount} или {_negativeInefficiencyPercentage:F2}%\n\n" +
                     $"🔸 <b>Показатели BTCUSDT</b> 🔸\n" +
                     $"• <b>Индекс перекупленности/перепроданности BTC</b>: {_btcOverSoldIndex:F2}\n" +
                     $"• <b>Краткосрочный TMO</b>: {_btcTmo30:F2}\n" +
                     $"• <b>Среднесрочный TMO</b>: {_btcTmo60:F2}\n" +
                     $"• <b>Долгосрочный TMO</b>: {_btcTmo180:F2}";

        await GlobalClients.TelegramBotService.SendMessageToChatAsync(chatId, report);
    }
}