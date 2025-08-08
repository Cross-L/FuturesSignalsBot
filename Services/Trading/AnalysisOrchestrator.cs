using System.Diagnostics;
using FuturesSignalsBot.Core;
using FuturesSignalsBot.Enums;
using FuturesSignalsBot.Services.Analysis;
using FuturesSignalsBot.Services.Notifiers;

namespace FuturesSignalsBot.Services.Trading;

public class AnalysisOrchestrator(
    IReadOnlyCollection<CryptocurrencyManagementService> cryptocurrencyManagementServices,
    CancellationTokenSource cancellationTokenSource)
{
    public static bool AreNotificationsPrepared { get; private set; }

    private const int CalculationBatchSize = 30;

    public async Task StartAsync()
    {
        try
        {
            Console.WriteLine("Получение данных...");
            await Task.WhenAll(cryptocurrencyManagementServices.Select(s => s.ReceiveTradingDataAsync()));

            var activeServices = GetActiveServices();
            Console.WriteLine($"Данные получены: {DateTimeOffset.UtcNow:dd.MM.yyyy HH:mm:ss zzz}");

            await Task.WhenAll(activeServices.Select(s => s.CalculateAsync()));
            Console.WriteLine($"Расчеты выполнены: {DateTimeOffset.UtcNow:dd.MM.yyyy HH:mm:ss zzz}");
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
                        await Task.WhenAll(activeServices.Select(s => s.UpdateDataAsync()));
                        activeServices = GetActiveServices();
                        Console.WriteLine(
                            $"Обновление данных завершено: {DateTimeOffset.UtcNow:dd.MM.yyyy HH:mm:ss zzz}");

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

    private static async Task CalculateInBatchesAsync(List<CryptocurrencyManagementService> services)
    {
        for (var i = 0; i < services.Count; i += CalculationBatchSize)
        {
            var batch = services.Skip(i).Take(CalculationBatchSize).Select(s => s.CalculateAsync());
            await Task.WhenAll(batch);
        }
    }

    private List<CryptocurrencyManagementService> GetActiveServices()
        => cryptocurrencyManagementServices
            .Where(s =>
                !s.Cryptocurrency.Deactivated &&
                AnalysisCore.Users.All(u =>
                    u.Value.DataService.Data.DisabledCurrencies.Contains(s.Cryptocurrency.Name) is not true))
            .ToList();


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

        foreach (var service in cryptocurrencyManagementServices.Where(service => service.Cryptocurrency.Deactivated))
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