using System.Text;
using System.Text.Json.Serialization;
using FuturesSignalsBot.Core;
using FuturesSignalsBot.Models;
using FuturesSignalsBot.Models.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FuturesSignalsBot.Services.Binance;

public static class MarketDataService
{
    private static readonly ApiRateLimiter DataReceivingLimiter =
        new(maxConcurrentRequests: 10, delay: TimeSpan.FromMilliseconds(100));

    private static readonly ApiRateLimiter DataUpdatingLimiter =
        new(maxConcurrentRequests: 30, delay: TimeSpan.FromMilliseconds(30));


    public static async Task<string?> GetCandleDataAsync(string symbol, string interval, int limit)
    {
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

                var queryString =
                    $"symbol={symbol}&interval={interval}&limit={limit}&startTime={startTimeUnix}&endTime={endTimeUnix}";
                var requestUrl = $"{endpoint}?{queryString}";

                using var response = await GlobalClients.HttpClientBigTimeout.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                // Capture detailed error information
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Failed to retrieve candle data for symbol: {symbol}, interval: {interval}, limit: {limit}. " +
                    $"HTTP Status: {response.StatusCode}, Reason: {response.ReasonPhrase}, Content: {errorContent}");
            }
            catch (HttpRequestException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error retrieving candle data for symbol: {symbol}, interval: {interval}, limit: {limit}. " +
                    $"Error: {ex.Message}", ex);
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
                var nowUtc = DateTimeOffset.UtcNow;
                var roundedTime = BinanceHttpHelper.RoundTimeToInterval(nowUtc, interval);
                var (intervalCount, intervalType) = BinanceHttpHelper.ParseInterval(interval);

                var lastCandleStartTime =
                    BinanceHttpHelper.CalculateStartTime(roundedTime, intervalCount, intervalType, 1);
                var startTime = lastCandleStartTime.ToUnixTimeMilliseconds();
                var endTime = roundedTime.ToUnixTimeMilliseconds();

                var queryString =
                    $"symbol={symbol}&interval={interval}&limit=1&startTime={startTime}&endTime={endTime}";
                var requestUrl = $"{endpoint}?{queryString}";

                using var response = await DataUpdatingLimiter.ExecuteAsync(async () =>
                    await GlobalClients.HttpClientShortTimeout.GetAsync(requestUrl));

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = new StringBuilder();
                errorMessage
                    .AppendLine($"Attempt {attempt}: GetLastCompletedCandleDataAsync: Ошибка при запросе данных.")
                    .AppendLine($"Символ: {symbol}")
                    .AppendLine($"Интервал: {interval}")
                    .AppendLine($"URL запроса: {requestUrl}")
                    .AppendLine($"Статус код: {(int)response.StatusCode} ({response.StatusCode})")
                    .AppendLine($"Причина: {response.ReasonPhrase}")
                    .AppendLine($"Содержимое ошибки: {errorContent}");

                errorMessages.Add(errorMessage.ToString());
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == CancellationToken.None)
            {
                var timeoutMessage = new StringBuilder();
                timeoutMessage
                    .AppendLine(
                        $"Attempt {attempt}: GetLastCompletedCandleDataAsync: Ошибка - истекло время ожидания запроса.")
                    .AppendLine($"Символ: {symbol}")
                    .AppendLine($"Интервал: {interval}");

                errorMessages.Add(timeoutMessage.ToString());
            }
            catch (Exception ex)
            {
                var detailedError = new StringBuilder();
                detailedError
                    .AppendLine($"Attempt {attempt}: GetLastCompletedCandleDataAsync: Произошла необработанная ошибка.")
                    .AppendLine($"Символ: {symbol}")
                    .AppendLine($"Интервал: {interval}")
                    .AppendLine($"Сообщение: {ex.Message}")
                    .AppendLine($"Тип исключения: {ex.GetType().FullName}");

                if (ex.InnerException != null)
                {
                    detailedError.AppendLine($"Внутреннее исключение: {ex.InnerException.Message}")
                        .AppendLine($"Тип внутреннего исключения: {ex.InnerException.GetType().FullName}");
                }

                detailedError.AppendLine("Стек вызовов:")
                    .AppendLine(ex.StackTrace ?? "Нет информации о стеке вызовов.");

                errorMessages.Add(detailedError.ToString());
            }

            if (attempt < maxRetries)
            {
                await Task.Delay(1000);
            }
        }

        foreach (var error in errorMessages)
        {
            await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(error);
        }

        var allErrors = string.Join(
            Environment.NewLine + new string('-', 50) + Environment.NewLine, errorMessages);
        throw new Exception(
            $"Не удалось получить данные для {symbol} с интервалом {interval} после {maxRetries} попыток. Ошибки:{Environment.NewLine}{allErrors}");
    }


    public static async Task LoadQuantitiesPrecision(List<Cryptocurrency> cryptocurrencies)
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://fapi.binance.com");

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