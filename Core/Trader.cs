using System.Collections.Concurrent;
using FuturesSignalsBot.Models;
using FuturesSignalsBot.Models.Config;
using FuturesSignalsBot.Services.Analysis;
using FuturesSignalsBot.Services.Binance;
using FuturesSignalsBot.Services.Bot;
using FuturesSignalsBot.Services.Trading;

namespace FuturesSignalsBot.Core;

public static class Trader
{
    private static List<CryptocurrencyTradingService> _cryptocurrencyTradingServices = [];

    private static CancellationTokenSource _cancellationTokenSource = new();
    public static ConcurrentDictionary<long, User> Users { get; } = [];

    private static TradingOrchestrator? _tradingOrchestrator;

    public static async Task Init(AppConfig appConfig)
    {
        GlobalClients.TelegramBotService = new TelegramBotService(appConfig);
        await AssignUsers(appConfig.UserConfigs);
    }

    public static async Task ExecuteTradeAsync()
    {
        await GlobalClients.TelegramBotService.Start();

        while (true)
        {
            await UpdateDailyCryptocurrencyList();

            _cryptocurrencyTradingServices = GlobalClients.CryptocurrenciesStorage.AllCryptocurrencies.Select
                (cryptocurrency => new CryptocurrencyTradingService(cryptocurrency, Users)).ToList();
            await MarketDataService.LoadQuantitiesPrecision(GlobalClients.CryptocurrenciesStorage.AllCryptocurrencies);
            
            var currentTime = DateTimeOffset.UtcNow;
            var cleanTime =
                new DateTimeOffset(currentTime.Year, currentTime.Month, currentTime.Day, 0, 0, 0, TimeSpan.Zero)
                    .AddDays(1);
            var delay = cleanTime - currentTime;
            var cleanTimeDelay = Task.Delay(delay, _cancellationTokenSource.Token);

            try
            {
                _tradingOrchestrator =
                    new TradingOrchestrator(_cryptocurrencyTradingServices, _cancellationTokenSource);

                var orchestratorTask = _tradingOrchestrator.StartAsync();
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
            }

            foreach (var cryptocurrencyTradingService in _cryptocurrencyTradingServices)
            {
                cryptocurrencyTradingService.ClearData();
            }

            BitcoinResistanceService.CandlesData.Clear();
            BitcoinResistanceService.ResistanceSeries.Clear();
        }
    }

    private static async Task UpdateDailyCryptocurrencyList()
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

        var currenciesToAdd = GlobalClients.CryptocurrenciesStorage.NewCurrencies.Select
            (currencyName => new Cryptocurrency(currencyName)).ToList();
        GlobalClients.CryptocurrenciesStorage.AllCryptocurrencies.AddRange(currenciesToAdd);
    }
    
    private static async Task AssignUsers(Dictionary<string, UserConfig> userConfigs)
    {
        foreach (var (id, userConfig) in userConfigs)
        {
            var parsedId = long.Parse(id);
            var username = await GlobalClients.TelegramBotService.GetUsernameByIdAsync(parsedId);
            
            var user = new User(username, userConfig.IsAdmin);
            
            if (!Users.TryAdd(parsedId, user))
            {
                Console.WriteLine($"Не удалось добавить пользователя {user.Name} ID: {parsedId}");
            }
        }
    }

    public static async Task<string> GetHealthStatus(long userId)
        => _tradingOrchestrator is not null
            ? await _tradingOrchestrator.GetServicesHealthReport(userId)
            : "Ожидание инициализации...";
}