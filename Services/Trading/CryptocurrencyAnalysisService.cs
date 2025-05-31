using System.Collections.Concurrent;
using FuturesSignalsBot.Core;
using FuturesSignalsBot.Indicators;
using FuturesSignalsBot.Indicators.Smoothing;
using FuturesSignalsBot.Models;
using FuturesSignalsBot.Services.Analysis;
using FuturesSignalsBot.Services.Binance;

namespace FuturesSignalsBot.Services.Trading;

public class CryptocurrencyAnalysisService
{
    private bool _newThirtyMinuteCandleReceived;
    
    private bool _initialized;

    public readonly Cryptocurrency Cryptocurrency;
    public LiquidationLevelAnalyzer LiquidationLevelAnalyzer { get; }
    public Exception? LastException { get; private set; }
    public bool TimeToUpdate => IsDataStale(Cryptocurrency.TradingDataContainer.FiveMinuteData, TimeSpan.FromMinutes(5));
    
    public CryptocurrencyAnalysisService(Cryptocurrency cryptocurrency)
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
                Cryptocurrency.Deactivated = true;
                return;
            }
            
            if (Cryptocurrency.Name.Equals("BTCUSDT", StringComparison.OrdinalIgnoreCase))
            {
                BitcoinResistanceService.CandlesData = Cryptocurrency.TradingDataContainer.FourHourData;
                BitcoinResistanceService.ResistanceSeries =
                    CorrelationTrendAnalyzer.CalculateBitcoinResistance(Cryptocurrency.TradingDataContainer.FourHourData);
                BitcoinResistanceService.ResistanceSeries.RemoveAll(series => series.FirstItem.Index == series.LastItem.Index);
            }
            
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

        var dataResponse = await MarketDataService.GetLastCompletedCandleDataAsync(Cryptocurrency.Name, interval);
        if (dataResponse == null)
            throw new Exception($"Не удалось получить последнюю {interval} свечу для {Cryptocurrency.Name}");

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
                    Cryptocurrency.Deactivated = true;
                    await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(
                        $"Валюта {Cryptocurrency.Name} отсутствует на Binance! Торговля на ней успешно остановлена");
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
                var prices = fiveMinuteData
                    .Skip(fiveMinuteData.Count - 72)
                    .Take(72)
                    .Select(data => data.Low)
                    .ToList();

                var score = ZScoreCalculator.CalculateScores(prices);
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
    
    private async Task HandleException(Exception ex)
    {
        Cryptocurrency.Deactivated = true;
        LastException = ex;
        Console.WriteLine($"{ex.Message}\n{ex.StackTrace}\n");
        await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(
            $"Ошибка в сервисе {Cryptocurrency.Name}:\n{ex.Message}\n{ex.StackTrace}\n\n" +
            "Работа сервиса остановлена, ожидается исправление...");
    }
}