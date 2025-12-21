using FuturesSignalsBot.Core;
using FuturesSignalsBot.Enums;
using FuturesSignalsBot.Models.IndicatorResults;
using FuturesSignalsBot.Services.Analysis;
using FuturesSignalsBot.Services.Binance;
using FuturesSignalsBot.Services.Notifiers;
using System.Diagnostics;

namespace FuturesSignalsBot.Services.Trading;

public class AnalysisOrchestrator(
    IReadOnlyCollection<CryptocurrencyManagementService> cryptocurrencyManagementServices,
    CancellationTokenSource cancellationTokenSource)
{
    public static bool AreNotificationsPrepared { get; private set; }

    private const int CalculationBatchSize = 30;

    private const int RequestBatchSize = 15;

    public async Task StartAsync()
    {
        try
        {
            var topTurnoverCurrencies = await MarketDataService.GetTopSymbolsByRolling24hTurnoverAsync(
                            [.. cryptocurrencyManagementServices.Select(s => s.Cryptocurrency)]
                        );

            var topSymbolsSet = topTurnoverCurrencies.Select(c => c.Name).ToHashSet();

            Console.WriteLine("Обновление статусов активности сервисов...");
            int deactivatedCount = 0;

            foreach (var service in cryptocurrencyManagementServices)
            {
                if (service.Cryptocurrency.Name == "BTCUSDT")
                {
                    service.Cryptocurrency.DeactivationReason = CurrencyDeactivationReason.None;
                    continue;
                }

                bool isTop = topSymbolsSet.Contains(service.Cryptocurrency.Name);

                if (isTop)
                {
                    if (service.Cryptocurrency.DeactivationReason == CurrencyDeactivationReason.NotInTop)
                    {
                        service.Cryptocurrency.DeactivationReason = CurrencyDeactivationReason.None;
                    }
                }
                else
                {
                    if (service.Cryptocurrency.DeactivationReason == CurrencyDeactivationReason.None)
                    {
                        service.Cryptocurrency.DeactivationReason = CurrencyDeactivationReason.NotInTop;
                        deactivatedCount++;
                    }
                }
            }

            Console.WriteLine($"[Info] Статусы обновлены. Деактивировано пар вне топа: {deactivatedCount}.");
            var activeServices = GetActiveServices();
            Console.WriteLine($"Получение данных для {activeServices.Count} активных пар...");

            await ProcessWithPriorityAndBatchingAsync(activeServices, s => s.ReceiveTradingDataAsync(), "Загрузка данных");
            Console.WriteLine($"Данные получены: {DateTimeOffset.UtcNow:dd.MM.yyyy HH:mm:ss zzz}");

            await CalculateInBatchesAsync(activeServices);
            Console.WriteLine($"Расчеты выполнены: {DateTimeOffset.UtcNow:dd.MM.yyyy HH:mm:ss zzz}");

            PrepareTradeNotifications(activeServices);
            await SendNotificationsAsync();

            PrepareTradeNotifications(activeServices);
            Console.WriteLine($"Предварительные операции завершены: {DateTimeOffset.UtcNow:dd.MM.yyyy HH:mm:ss zzz}");
            await SendNotificationsAsync();

            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    activeServices = GetActiveServices();

                    if (activeServices.All(service => service.TimeToUpdate))
                    {
                        await ProcessWithPriorityAndBatchingAsync(activeServices, s => s.UpdateDataAsync(), "Обновление данных");
                        activeServices = GetActiveServices();
                        Console.WriteLine($"Обновление данных завершено: {DateTimeOffset.UtcNow:dd.MM.yyyy HH:mm:ss zzz}");

                        await CalculateInBatchesAsync(activeServices);
                        Console.WriteLine($"Расчеты выполнены: {DateTimeOffset.UtcNow:dd.MM.yyyy HH:mm:ss zzz}");

                        PrepareTradeNotifications(activeServices);
                        await SendNotificationsAsync();
                    }

                    await Task.Delay(3000, cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка в оркестраторе: {ex.Message}\n{ex.StackTrace}");
                }
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при инициализации: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static async Task ProcessWithPriorityAndBatchingAsync(
        List<CryptocurrencyManagementService> services,
        Func<CryptocurrencyManagementService, Task> action,
        string operationName)
    {
        var btc = services.FirstOrDefault(s =>
                s.Cryptocurrency.Name.Equals("BTCUSDT", StringComparison.OrdinalIgnoreCase));

        var otherBatches = services
                .Where(s => s != btc)
                .Chunk(RequestBatchSize);

        int totalCount = services.Count;
        int processedCount = 0;

        if (btc is not null)
        {
            await action(btc);
            processedCount++;
        }

        foreach (var batch in otherBatches)
        {
            await Task.WhenAll(batch.Select(action));
            processedCount += batch.Length;
            Console.WriteLine($"[{operationName}] Обработано {processedCount} из {totalCount} валют...");
        }
    }

    private static async Task CalculateInBatchesAsync(List<CryptocurrencyManagementService> services)
    {
        for (var i = 0; i < services.Count; i += CalculationBatchSize)
        {
            var batch = services.Skip(i).Take(CalculationBatchSize).Select(s => s.CalculateAsync());
            await Task.WhenAll(batch);
        }
    }

    private List<CryptocurrencyManagementService> GetActiveServices()
        => [.. cryptocurrencyManagementServices
            .Where(s =>
                !s.Cryptocurrency.Deactivated &&
                AnalysisCore.Users.All(u =>
                    u.Value.DataService.Data.DisabledCurrencies.Contains(s.Cryptocurrency.Name) is not true))];


    private static void PrepareTradeNotifications(List<CryptocurrencyManagementService> activeServices)
    {
        var allResistanceInfos = activeServices
            .Where(service => service.Cryptocurrency.Name != "BTCUSDT")
            .Select(service => service.Cryptocurrency.TradingDataContainer.ResistanceInfos)
            .ToList();

        CorrelationTrendAnalyzer.CalculateCorrelationTrends(allResistanceInfos);
        TmoIndexAnalyzer.AnalyzeTmoIndicators(activeServices);

        var liquidationLevelAnalyzers = activeServices
            .Select(service => service.LiquidationLevelAnalyzer)
            .ToList();
        var preliminaryImpulses30M = liquidationLevelAnalyzers
            .Select(analyzer => analyzer.PreliminaryImpulse30M)
            .ToList();
        var preliminaryImpulses5M = liquidationLevelAnalyzers
            .Select(analyzer => analyzer.PreliminaryImpulse5M)
            .OfType<PreliminaryImpulse>()
            .ToList();
        var specifiedPreliminaryImpulses = liquidationLevelAnalyzers
            .Select(analyzer => analyzer.SpecifiedPreliminaryImpulse5M)
            .ToList();

        MarketAbsorptionAnalyzer.AnalyzeLastAbsorption(activeServices, preliminaryImpulses5M);
        TmoIndexAnalyzer.AnalyzeTmoInefficiency(activeServices, preliminaryImpulses30M, preliminaryImpulses5M,
            specifiedPreliminaryImpulses);
        
        PreliminaryImpulseAnalyzer.UpdateTopLists(preliminaryImpulses5M);
        UniqueGeneralAnalyzer.AnalyzeDataList();
        UniqueGeneralAnalyzer.AnalyzeImpulses(preliminaryImpulses30M!);
        MarketFundingAnalyzer.AnalyzeFunding(activeServices);
        MarketFundingAnalyzer.AnalyzeFundingLists(activeServices, preliminaryImpulses5M);
        AreNotificationsPrepared = true;
    }

    private static async Task SendNotificationsAsync()
    {
        try
        {
            await UniqueGeneralAnalyzer.SendReport();
            await LiquidationNotifier.SendTopLiquidationData(LiquidationLevelTopType.LongLiquidation);
            await LiquidationNotifier.SendTopLiquidationData(LiquidationLevelTopType.ShortLiquidation);
            await LiquidationNotifier.SendTopLiquidationData(LiquidationLevelTopType.BestLongs);
            await LiquidationNotifier.SendTopLiquidationData(LiquidationLevelTopType.BestShorts);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при опросе: {ex.Message}\nСтрока: {ex.StackTrace}");
        }
    }

    public async Task<string> GetServicesHealthReport(long userId)
    {
        var stableServicesCount = cryptocurrencyManagementServices.Count(service => !service.Cryptocurrency.Deactivated);

        var errorServices = cryptocurrencyManagementServices
            .Where(service => service.Cryptocurrency.DeactivationReason == CurrencyDeactivationReason.Error);

        foreach (var service in errorServices)
        {
            if (service.LastException != null)
            {
                var exception = service.LastException;
                var message =
                    $"Ошибка в сервисе {service.Cryptocurrency.Name}: {exception.GetType().Name} - {exception.Message}";

                var stackTrace = new StackTrace(exception, true);

                var relevantFrame = stackTrace.GetFrames().FirstOrDefault(frame =>
                    !frame.GetMethod()!.Module.Name.StartsWith("System.") &&
                    !frame.GetMethod()!.Module.Name.StartsWith("Microsoft.") &&
                    frame.GetFileLineNumber() > 0);

                if (relevantFrame != null)
                {
                    message += $"\nКласс: {relevantFrame.GetMethod()!.DeclaringType?.FullName}, " +
                               $"Строка: {relevantFrame.GetFileLineNumber()}";
                }

                await GlobalClients.TelegramBotService.SendMessageToChatAsync(userId, message);
            }
        }

        return $"{stableServicesCount} из {cryptocurrencyManagementServices.Count}";
    }
}