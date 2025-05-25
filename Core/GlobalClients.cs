using System;
using System.Net.Http;
using FuturesSignalsBot.Services.Bot;
using FuturesSignalsBot.Storage;

namespace FuturesSignalsBot.Core;

public static class GlobalClients
{
    public static readonly CryptocurrenciesStorage CryptocurrenciesStorage = new();
        
    public static TelegramBotService TelegramBotService;

    public static readonly HttpClient HttpClientBigTimeout;
    public static readonly HttpClient HttpClientShortTimeout;

    static GlobalClients()
    {
        var httpHandler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 50
        };

        HttpClientBigTimeout = new HttpClient(httpHandler)
        {
            BaseAddress = new Uri("https://fapi.binance.com"),
            Timeout = TimeSpan.FromSeconds(1000)
        };

        HttpClientShortTimeout = new HttpClient(httpHandler)
        {
            BaseAddress = new Uri("https://fapi.binance.com"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

}