using System.Collections.Concurrent;
using FuturesSignalsBot.Models.Config;
using FuturesSignalsBot.Services.Analysis;
using FuturesSignalsBot.Services.Binance;
using FuturesSignalsBot.Services.Bot;
using FuturesSignalsBot.Services.Trading;

namespace FuturesSignalsBot.Core;

public static class AnalysisCore
{
    private static List<CryptocurrencyManagementService> _cryptocurrencyManagementServices = [];

    private static CancellationTokenSource _cancellationTokenSource = new();
    public static ConcurrentDictionary<long, User> Users { get; } = [];

    private static AnalysisOrchestrator? _analysisOrchestrator;

    public static async Task Init(AppConfig appConfig)
    {
        GlobalClients.TelegramBotService = new TelegramBotService(appConfig);
        await AssignUsersAsync(appConfig.UserConfigs);
    }

    public static async Task ExecuteTradeAsync()
    {
        await GlobalClients.TelegramBotService.Start();

        while (true)
        {
            await UpdateCurrenciesAsync();

            _cryptocurrencyManagementServices = GlobalClients.CryptocurrenciesStorage.AllCryptocurrencies.Select
                (cryptocurrency => new CryptocurrencyManagementService(cryptocurrency)).ToList();
            await MarketDataService.LoadQuantitiesPrecision(GlobalClients.CryptocurrenciesStorage.AllCryptocurrencies);
            
            var currentTime = DateTimeOffset.UtcNow;
            var cleanTime =
                new DateTimeOffset(currentTime.Year, currentTime.Month, currentTime.Day, 0, 0, 0, TimeSpan.Zero)
                    .AddDays(1);
            var delay = cleanTime - currentTime;
            var cleanTimeDelay = Task.Delay(delay, _cancellationTokenSource.Token);

            try
            {
                _analysisOrchestrator =
                    new AnalysisOrchestrator(_cryptocurrencyManagementServices, _cancellationTokenSource);

                var orchestratorTask = _analysisOrchestrator.StartAsync();
                var completedTask = await Task.WhenAny(orchestratorTask, cleanTimeDelay);

                if (completedTask == cleanTimeDelay)
                {
                    await _cancellationTokenSource.CancelAsync();
                    await orchestratorTask;
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Задачи успешно перезапущены!");
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
                await SaveUsersDataAsync();
            }

            foreach (var cryptocurrencyTradingService in _cryptocurrencyManagementServices)
            {
                cryptocurrencyTradingService.ClearData();
            }

            BitcoinResistanceService.CandlesData.Clear();
            BitcoinResistanceService.ResistanceSeries.Clear();
        }
    }

    private static async Task UpdateCurrenciesAsync()
    {
        var removedCurrencies = GlobalClients.CryptocurrenciesStorage.AllCryptocurrencies
            .Where(currency => currency.Deactivated)
            .Select(currency => currency.Name)
            .ToList();
        GlobalClients.CryptocurrenciesStorage.AllCryptocurrencies.RemoveAll(currency => currency.Deactivated);

        if (removedCurrencies.Count != 0)
        {
            await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(
                $"Валюты {string.Join(", ", removedCurrencies)} убраны из списка");
        }
    }
    
    private static async Task AssignUsersAsync(Dictionary<string, UserConfig> userConfigs)
    {
        foreach (var (id, userConfig) in userConfigs)
        {
            var parsedId = long.Parse(id);
            var username = await GlobalClients.TelegramBotService.GetUsernameByIdAsync(parsedId);
            
            var user = new User(parsedId, username, userConfig.IsAdmin);
            
            if (!Users.TryAdd(parsedId, user))
            {
                Console.WriteLine($"Не удалось добавить пользователя {user.Name} ID: {parsedId}");
            }

            await user.DataService.LoadUserDataAsync();
        }
    }
    
    public static async Task SaveUsersDataAsync()
    {
        foreach (var user in Users)
        {
            await user.Value.DataService.SaveUserDataAsync();
        }

        Console.WriteLine("Данные пользователей сохранены");
    }

    public static async Task<string> GetHealthStatus(long userId)
        => _analysisOrchestrator is not null
            ? await _analysisOrchestrator.GetServicesHealthReport(userId)
            : "Ожидание инициализации...";
}