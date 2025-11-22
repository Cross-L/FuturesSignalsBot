using FuturesSignalsBot.Core;
using FuturesSignalsBot.Models;
using Newtonsoft.Json.Linq;

namespace FuturesSignalsBot.Services.Binance;

public static class MarketDataService
{
    private static readonly ApiRateLimiter DataReceivingLimiter =
        new(maxConcurrentRequests: 5, delay: TimeSpan.FromMilliseconds(250));

    private static readonly ApiRateLimiter DataUpdatingLimiter =
        new(maxConcurrentRequests: 10, delay: TimeSpan.FromMilliseconds(100));

    private const int MaxWeightPerMinute = 2400;
    private const int SafeWeightThreshold = 2100;
    private static volatile int _currentUsedWeight = 0;
    private static readonly Lock WeightLock = new();

    public static async Task<string?> GetCandleDataAsync(string symbol, string interval, int limit)
    {
        await WaitForWeightCapacityAsync();

        return await DataReceivingLimiter.ExecuteAsync(async () =>
        {
            const string endpoint = "/fapi/v1/klines";
            try
            {
                var nowUtc = DateTimeOffset.UtcNow;
                var roundedTime = BinanceHttpHelper.RoundTimeToInterval(nowUtc, interval);
                var (intervalCount, intervalType) = BinanceHttpHelper.ParseInterval(interval);

                var endTimeUnix = roundedTime.ToUnixTimeMilliseconds();
                var startTimeUnix = BinanceHttpHelper
                    .CalculateStartTime(roundedTime, intervalCount, intervalType, limit)
                    .ToUnixTimeMilliseconds();

                var queryString = $"symbol={symbol}&interval={interval}&limit={limit}&startTime={startTimeUnix}&endTime={endTimeUnix}";
                var requestUrl = $"{endpoint}?{queryString}";

                using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                using var response = await GlobalClients.HttpClientBigTimeout.SendAsync(request);

                UpdateWeightFromHeaders(response.Headers);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                if ((int)response.StatusCode == 429)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 60;
                    var msg = $"!!! RATE LIMIT EXCEEDED (429). Server asks to wait {retryAfter}s. Pausing global operations.";
                    Console.WriteLine(msg);
                    await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(msg);

                    lock (WeightLock) { _currentUsedWeight = MaxWeightPerMinute + 100; }

                    await Task.Delay(TimeSpan.FromSeconds(retryAfter));

                    lock (WeightLock) { _currentUsedWeight = 0; }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Failed to retrieve candle data for symbol: {symbol}. HTTP {response.StatusCode}. Content: {errorContent}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetCandleDataAsync for {symbol}: {ex.Message}", ex);
            }
        });
    }

    public static async Task<string> GetLastCompletedCandleDataAsync(string symbol, string interval)
    {
        const string endpoint = "/fapi/v1/klines";
        const int maxRetries = 3;
        var attempt = 0;
        var errorMessages = new List<string>();

        while (attempt < maxRetries)
        {
            attempt++;
            try
            {
                await WaitForWeightCapacityAsync();

                var nowUtc = DateTimeOffset.UtcNow;
                var roundedTime = BinanceHttpHelper.RoundTimeToInterval(nowUtc, interval);
                var (intervalCount, intervalType) = BinanceHttpHelper.ParseInterval(interval);
                var lastCandleStartTime = BinanceHttpHelper.CalculateStartTime(roundedTime, intervalCount, intervalType, 1);

                var startTime = lastCandleStartTime.ToUnixTimeMilliseconds();
                var endTime = roundedTime.ToUnixTimeMilliseconds();
                var queryString = $"symbol={symbol}&interval={interval}&limit=1&startTime={startTime}&endTime={endTime}";
                var requestUrl = $"{endpoint}?{queryString}";

                using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                using var response = await DataUpdatingLimiter.ExecuteAsync(async () =>
                    await GlobalClients.HttpClientShortTimeout.SendAsync(request));

                UpdateWeightFromHeaders(response.Headers);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                if ((int)response.StatusCode == 418 || (int)response.StatusCode == 429)
                {
                    var retryStr = response.Headers.RetryAfter?.Delta?.TotalSeconds.ToString() ?? "unknown";
                    throw new Exception($"IP BANNED or Rate Limit. Retry after: {retryStr}");
                }

                errorMessages.Add($"Status: {response.StatusCode}, Reason: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                errorMessages.Add($"Attempt {attempt} error: {ex.Message}");
            }

            if (attempt < maxRetries) await Task.Delay(1000);
        }

        throw new Exception($"Failed GetLastCompletedCandleDataAsync for {symbol}: {string.Join("; ", errorMessages)}");
    }

    private static void UpdateWeightFromHeaders(System.Net.Http.Headers.HttpResponseHeaders headers)
    {
        if (headers.TryGetValues("x-mbx-used-weight-1m", out var values))
        {
            var weightStr = values.FirstOrDefault();
            if (int.TryParse(weightStr, out int weight))
            {
                lock (WeightLock)
                {
                    _currentUsedWeight = weight;
                }
            }
        }
    }

    private static async Task WaitForWeightCapacityAsync()
    {
        int weight;
        lock (WeightLock)
        {
            weight = _currentUsedWeight;
        }

        if (weight >= SafeWeightThreshold)
        {

            Console.WriteLine($"[RATE LIMIT GUARD] Current Weight: {weight}/{MaxWeightPerMinute}. Pausing requests for 5s...");
            await Task.Delay(5000);

            lock (WeightLock)
            {
                if (_currentUsedWeight >= SafeWeightThreshold)
                    _currentUsedWeight = SafeWeightThreshold - 100;
            }
        }
    }


    public static async Task LoadQuantitiesPrecision(List<Cryptocurrency> cryptocurrencies)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://fapi.binance.com")
        };

        const string url = "https://fapi.binance.com/fapi/v1/exchangeInfo";
        var response = await httpClient.GetAsync(url);
        var responseBody = await response.Content.ReadAsStringAsync();
        var jObject = JObject.Parse(responseBody);

        foreach (var cryptocurrency in cryptocurrencies)
        {
            var symbolInfo = jObject["symbols"]!.FirstOrDefault(s => s["symbol"]!.ToString() == cryptocurrency.Name);
            var lotSizeInfo = symbolInfo?["filters"]!.FirstOrDefault(f => f["filterType"]!.ToString() == "LOT_SIZE");

            if (lotSizeInfo != null)
            {
                var stepSize = lotSizeInfo["stepSize"]?.ToString();
                cryptocurrency.QuantityPrecision = BinanceHttpHelper.CalculatePrecision(stepSize!);
            }
        }
    }
}