using System.Collections.Concurrent;
using FuturesSignalsBot.Core;
using FuturesSignalsBot.Enums;
using FuturesSignalsBot.Indicators;
using FuturesSignalsBot.Indicators.Smoothing;
using FuturesSignalsBot.Models;
using FuturesSignalsBot.Services.Analysis;
using FuturesSignalsBot.Services.Binance;

namespace FuturesSignalsBot.Services.Trading;

public class CryptocurrencyManagementService
{
    private bool _newThirtyMinuteCandleReceived;
    
    private bool _initialized;

    public readonly Cryptocurrency Cryptocurrency;
    public LiquidationLevelAnalyzer LiquidationLevelAnalyzer { get; }
    public Exception? LastException { get; private set; }
    public bool TimeToUpdate => IsDataStale(Cryptocurrency.TradingDataContainer.FiveMinuteData, TimeSpan.FromMinutes(5));
    
    public CryptocurrencyManagementService(Cryptocurrency cryptocurrency)
    {
        Cryptocurrency = cryptocurrency;
        LiquidationLevelAnalyzer = new LiquidationLevelAnalyzer(Cryptocurrency);
    }

    public void ClearData() => Cryptocurrency.TradingDataContainer.Clear();

    public async Task ReceiveTradingDataAsync()
    {
        try
        {
            var fourHourResponse = await MarketDataService.GetCandleDataAsync(Cryptocurrency.Name, "4h", 500);
            var thirtyMinuteResponse = await MarketDataService.GetCandleDataAsync(Cryptocurrency.Name, "30m", 1000);
            var fiveMinuteResponse = await MarketDataService.GetCandleDataAsync(Cryptocurrency.Name, "5m", 500);

            if (fourHourResponse == null)
                throw new Exception($"Не удалось получить 4-х часовые данные для {Cryptocurrency.Name}");
            if (thirtyMinuteResponse == null)
                throw new Exception($"Не удалось получить 30-минутные данные для {Cryptocurrency.Name}");
            if (fiveMinuteResponse == null)
                throw new Exception($"Не удалось получить 5-минутные данные для {Cryptocurrency.Name}");

            Cryptocurrency.TradingDataContainer.FourHourData =
                CryptocurrencyResponseParser.GetCryptocurrencyDataFromResponse(fourHourResponse);
            Cryptocurrency.TradingDataContainer.ThirtyMinuteData =
                CryptocurrencyResponseParser.GetCryptocurrencyDataFromResponse(thirtyMinuteResponse);
            Cryptocurrency.TradingDataContainer.FiveMinuteData =
                CryptocurrencyResponseParser.GetCryptocurrencyDataFromResponse(fiveMinuteResponse);

            if (Cryptocurrency.TradingDataContainer.FiveMinuteData.Last().Volume is 0)
            {
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(
                    $"Валюта {Cryptocurrency.Name} отсутствует на Binance! Торговля на ней успешно остановлена");
                Cryptocurrency.DeactivationReason = CurrencyDeactivationReason.Delisted;
                return;
            }
            
            if (Cryptocurrency.Name.Equals("BTCUSDT", StringComparison.OrdinalIgnoreCase))
            {
                BitcoinResistanceService.CandlesData = Cryptocurrency.TradingDataContainer.FourHourData;
                BitcoinResistanceService.ResistanceSeries =
                    CorrelationTrendAnalyzer.CalculateBitcoinResistance(Cryptocurrency.TradingDataContainer.FourHourData);
                BitcoinResistanceService.ResistanceSeries.RemoveAll(series => series.FirstItem.Index == series.LastItem.Index);
            }

            await InitializeFundingHistoryAsync();
            _newThirtyMinuteCandleReceived = true;
            // Console.WriteLine($"Получены данные для валюты {Cryptocurrency.Name}");
        }
        catch (Exception ex)
        {
            await HandleException(ex);
        }
    }
        
    public async Task UpdateDataAsync()
    {
        try
        {
            var updateTasks = new List<Task>
            {
                UpdateTimeframeDataAsync("4h", Cryptocurrency.TradingDataContainer.FourHourData, TimeSpan.FromHours(4)),
                UpdateTimeframeDataAsync("30m", Cryptocurrency.TradingDataContainer.ThirtyMinuteData, TimeSpan.FromMinutes(30)),
                UpdateTimeframeDataAsync("5m", Cryptocurrency.TradingDataContainer.FiveMinuteData, TimeSpan.FromMinutes(5))
            };

            await Task.WhenAll(updateTasks);
        }
        catch (Exception ex)
        {
            await HandleException(ex);
        }
    }
        
    private async Task UpdateTimeframeDataAsync(string interval, List<CryptocurrencyDataItem> dataList, TimeSpan threshold)
    {
        if (!IsDataStale(dataList, threshold))
            return;

        var dataResponse = await MarketDataService.GetLastCompletedCandleDataAsync(Cryptocurrency.Name, interval) 
            ?? throw new Exception($"Не удалось получить последнюю {interval} свечу для {Cryptocurrency.Name}");
        var lastItem = dataList.LastOrDefault();
        var newDataItem = await CryptocurrencyResponseParser.GetSingleCryptocurrencyDataItemFromResponseAsync(
            dataResponse, Cryptocurrency.Name, (lastItem?.Index ?? -1) + 1);

        if (lastItem == null || newDataItem.OpenTime > lastItem.CloseTime)
        {
            dataList.Add(newDataItem);
            LsmaSmoothing.SmoothLastItem(dataList);

            if (interval.Equals("4h", StringComparison.OrdinalIgnoreCase))
            {
                if (Cryptocurrency.Name.Equals("BTCUSDT", StringComparison.OrdinalIgnoreCase))
                {
                    BitcoinResistanceService.CandlesData = dataList;
                    BitcoinResistanceService.ResistanceSeries =
                        CorrelationTrendAnalyzer.CalculateBitcoinResistance(dataList);
                }
            }
            else if (interval.Equals("30m", StringComparison.OrdinalIgnoreCase))
            {
                _newThirtyMinuteCandleReceived = true;
                
                if (newDataItem.Volume is 0)
                {
                    Cryptocurrency.DeactivationReason = CurrencyDeactivationReason.Delisted;
                    await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(
                        $"Валюта {Cryptocurrency.Name} отсутствует на Binance! Торговля на ней успешно остановлена");
                }
                else
                {
                    await UpdateFundingWithReversalCheck();
                }
            }
        }
    }
        
    private static bool IsDataStale(IEnumerable<CryptocurrencyDataItem> dataList, TimeSpan threshold)
    {
        var lastItem = dataList.LastOrDefault();
        var currentTime = DateTimeOffset.UtcNow;
        return lastItem == null || currentTime - lastItem.CloseTime.UtcDateTime >= threshold;
    }

    public async Task CalculateAsync()
    {
        if (Cryptocurrency.Deactivated)
            return;

        try
        {
            if (_initialized)
            {
                if (_newThirtyMinuteCandleReceived)
                {
                    CryptocurrencyAnalysisEngine.CalculateLastItemIndicators(Cryptocurrency);
                }

                var fiveMinuteData = Cryptocurrency.TradingDataContainer.FiveMinuteData;
                var allLowPrices = fiveMinuteData.Select(data => data.Low).ToList();
                int lastIndex = fiveMinuteData.Count - 1;
                var score = ZScoreCalculator.CalculateScores(allLowPrices, lastIndex, 72, 72);
                fiveMinuteData.Last().Score = score;

                LiquidationLevelAnalyzer.Update(_newThirtyMinuteCandleReceived);
            }
            else
            {
                CryptocurrencyAnalysisEngine.InitializeIndicators(Cryptocurrency);
                LiquidationLevelAnalyzer.InitializeAsync();
                _initialized = true;
            }
            
            _newThirtyMinuteCandleReceived = false;
                
            if (!Cryptocurrency.Name.Equals("BTCUSDT", StringComparison.OrdinalIgnoreCase))
            {
                var resistanceTasks = BitcoinResistanceService.ResistanceSeries.Select(series =>
                    CorrelationTrendAnalyzer.ProcessOppositeTrend(
                        Cryptocurrency.Name,
                        BitcoinResistanceService.CandlesData,
                        Cryptocurrency.TradingDataContainer.FourHourData,
                        series));
    
                var resistanceInfos = await Task.WhenAll(resistanceTasks);
                Cryptocurrency.TradingDataContainer.ResistanceInfos.AddRange(
                    resistanceInfos.Where(info => info != null).Select(info => info!));
            }
        }
        catch (Exception ex)
        {
            await HandleException(ex);
        }
    }

    private async Task InitializeFundingHistoryAsync()
    {
        var currentRate = await MarketDataService.GetCurrentFundingRateAsync(Cryptocurrency.Name);
        var history = await MarketDataService.GetLastHistoricalFundingRateAsync(Cryptocurrency.Name);

        if (history.HasValue)
        {
            Cryptocurrency.PreviousFundingRate = history.Value.Rate;
            Cryptocurrency.FundingRate = currentRate;

            bool turnedNegative = Cryptocurrency.PreviousFundingRate > 0 && currentRate < 0;
            bool turnedPositive = Cryptocurrency.PreviousFundingRate < 0 && currentRate > 0;

            if (turnedNegative || turnedPositive)
            {
                Cryptocurrency.FundingReversalTime = history.Value.Time;
            }
        }
        else
        {
            Cryptocurrency.FundingRate = currentRate;
        }
    }

    private async Task UpdateFundingWithReversalCheck()
    {
        var newRate = await MarketDataService.GetCurrentFundingRateAsync(Cryptocurrency.Name);

        if (Cryptocurrency.FundingRate != 0)
        {
            bool turnedNegative = Cryptocurrency.FundingRate > 0 && newRate < 0;
            bool turnedPositive = Cryptocurrency.FundingRate < 0 && newRate > 0;

            if (turnedNegative || turnedPositive)
            {
                Cryptocurrency.FundingReversalTime = DateTimeOffset.UtcNow;
                Cryptocurrency.PreviousFundingRate = Cryptocurrency.FundingRate;
            }
        }

        Cryptocurrency.FundingRate = newRate;
    }

    private async Task HandleException(Exception ex)
    {
        Cryptocurrency.DeactivationReason = CurrencyDeactivationReason.Error;
        LastException = ex;
        Console.WriteLine($"{ex.Message}\n{ex.StackTrace}\n");
        await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(
            $"Ошибка в сервисе {Cryptocurrency.Name}:\n{ex.Message}\n{ex.StackTrace}\n\n" +
            "Работа сервиса остановлена, ожидается исправление...");
    }
}