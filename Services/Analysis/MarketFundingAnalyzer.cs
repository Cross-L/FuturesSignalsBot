using FuturesSignalsBot.Core;
using FuturesSignalsBot.Models.IndicatorResults;
using FuturesSignalsBot.Services.Trading;

namespace FuturesSignalsBot.Services.Analysis;

public static class MarketFundingAnalyzer
{
    private static int _negativeFundingCount;
    private static decimal _negativeFundingPercentage;
    private static int _positiveFundingCount;
    private static decimal _positiveFundingPercentage;

    private static decimal _btcPriceChange;
    private static decimal _btcCurrentFunding;
    private static decimal _ethPriceChange;
    private static decimal _ethCurrentFunding;

    private static int _totalActiveCount;

    public static List<PreliminaryImpulse> TopByLongFunding { get; private set; } = [];
    public static List<PreliminaryImpulse> TopByShortFunding { get; private set; } = [];

    public static List<PreliminaryImpulse> TopByLongReverse { get; private set; } = [];
    public static List<PreliminaryImpulse> TopByShortReverse { get; private set; } = [];

    public static void AnalyzeFunding(List<CryptocurrencyManagementService> activeServices)
    {
        _totalActiveCount = activeServices.Count;
        if (_totalActiveCount == 0) return;

        _negativeFundingCount = activeServices.Count(s => s.Cryptocurrency.FundingRate < 0);
        _positiveFundingCount = activeServices.Count(s => s.Cryptocurrency.FundingRate > 0);

        _negativeFundingPercentage = (decimal)_negativeFundingCount / _totalActiveCount * 100;
        _positiveFundingPercentage = (decimal)_positiveFundingCount / _totalActiveCount * 100;

        var btcService = activeServices.FirstOrDefault(s => s.Cryptocurrency.Name == "BTCUSDT");
        var ethService = activeServices.FirstOrDefault(s => s.Cryptocurrency.Name == "ETHUSDT");

        if (btcService != null)
        {
            _btcPriceChange = CalculatePriceChangeFromStartOfDay(btcService);
            _btcCurrentFunding = btcService.Cryptocurrency.FundingRate;
        }

        if (ethService != null)
        {
            _ethPriceChange = CalculatePriceChangeFromStartOfDay(ethService);
            _ethCurrentFunding = ethService.Cryptocurrency.FundingRate;
        }
    }

    private static decimal CalculatePriceChangeFromStartOfDay(CryptocurrencyManagementService service)
    {
        var data = service.Cryptocurrency.TradingDataContainer.FiveMinuteData;
        if (data == null || data.Count == 0) return 0m;

        var todayUtc = DateTimeOffset.UtcNow.Date;

        var startCandle = data.FirstOrDefault(c => c.OpenTime.UtcDateTime == todayUtc);
        var lastCandle = data.Last();

        if (startCandle == null) return 0m;
        return (lastCandle.Close - startCandle.Open) / startCandle.Open * 100;
    }

    public static void AnalyzeFundingLists(List<CryptocurrencyManagementService> activeServices, List<PreliminaryImpulse> impulses5M)
    {
        var servicesDict = activeServices.ToDictionary(s => s.Cryptocurrency.Name, s => s.Cryptocurrency);

        TopByLongFunding = impulses5M
            .Where(imp => servicesDict.TryGetValue(imp.Currency, out var crypto) && crypto.FundingRate < 0)
            .OrderBy(imp => imp.ChangeOv)
            .Take(10)
            .ToList();

        TopByShortFunding = impulses5M
            .Where(imp => servicesDict.TryGetValue(imp.Currency, out var crypto) && crypto.FundingRate > 0)
            .OrderByDescending(imp => imp.ChangeOv)
            .Take(10)
            .ToList();

        TopByLongReverse = impulses5M
        .Where(imp => servicesDict.TryGetValue(imp.Currency, out var crypto)
               && crypto.FundingRate < 0
               && crypto.PreviousFundingRate > 0)
        .OrderBy(imp => imp.ChangeOv)
        .Take(10).ToList();

        TopByShortReverse = impulses5M
            .Where(imp => servicesDict.TryGetValue(imp.Currency, out var crypto)
                   && crypto.FundingRate > 0
                   && crypto.PreviousFundingRate < 0)
            .OrderByDescending(imp => imp.ChangeOv)
            .Take(10).ToList();
    }

    public static async Task SendFundingReportAsync(long chatId)
    {
        var report = $"🗿<b>Фон рынка/фандинг</b>\n\n" +
                     $"• <b>Фандинг(-)</b>: {_negativeFundingPercentage:F0}% или {_negativeFundingCount}\n" +
                     $"• <b>Фандинг(+)</b>: {_positiveFundingPercentage:F0}% или {_positiveFundingCount}\n" +
                     $"• <b>ВТС</b>: {(_btcPriceChange >= 0 ? "+" : "")}{_btcPriceChange:F2}% / {(_btcCurrentFunding >= 0 ? "+" : "")}{_btcCurrentFunding:F10}\n" +
                     $"• <b>ЕТС</b>: {(_ethPriceChange >= 0 ? "+" : "")}{_ethPriceChange:F2}% / {(_ethCurrentFunding >= 0 ? "+" : "")}{_ethCurrentFunding:F10}";

        await GlobalClients.TelegramBotService.SendMessageToChatAsync(chatId, report);
    }
}