using System.Globalization;
using System.Text.Json;
using FuturesSignalsBot.Core;
using FuturesSignalsBot.Models;

namespace FuturesSignalsBot.Services.Trading;

public static class CryptocurrencyResponseParser
{
    public static List<CryptocurrencyDataItem> GetCryptocurrencyDataFromResponse(string response)
    {
        var jsonDocument = JsonDocument.Parse(response);
        var data = jsonDocument.RootElement.EnumerateArray().ToList();
        var cryptocurrencyData = new List<CryptocurrencyDataItem>();
        var startIndex = 0;

        foreach (var item in data)
        {
            var rowData = item.EnumerateArray().ToArray();

            var tableData = new CryptocurrencyDataItem
            {
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(rowData[0].GetInt64()).UtcDateTime,
                Open = decimal.Parse(rowData[1].GetString()!, CultureInfo.InvariantCulture),
                High = decimal.Parse(rowData[2].GetString()!, CultureInfo.InvariantCulture),
                Low = decimal.Parse(rowData[3].GetString()!, CultureInfo.InvariantCulture),
                Close = decimal.Parse(rowData[4].GetString()!, CultureInfo.InvariantCulture),
                Volume = decimal.Parse(rowData[5].GetString()!, CultureInfo.InvariantCulture),
                CloseTime = DateTimeOffset.FromUnixTimeMilliseconds(rowData[6].GetInt64()).UtcDateTime,
                Index = startIndex
            };

            cryptocurrencyData.Add(tableData);
            startIndex++;
        }

        return cryptocurrencyData;
    }
    
    public static async Task<CryptocurrencyDataItem> GetSingleCryptocurrencyDataItemFromResponseAsync(
        string response, string cryptocurrencyName, int index)
    {
        try
        {
            using var jsonDocument = JsonDocument.Parse(response);
            var root = jsonDocument.RootElement;

            // 1. Проверяем, что корень — массив и он не пуст.
            if (root.ValueKind != JsonValueKind.Array)
            {
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(
                    $"Ошибка в сервисе {cryptocurrencyName}:\n" +
                    "Ожидался массив JSON в корне, но получен другой тип.\n" +
                    $"Полный ответ:\n{response}\n\n" +
                    "Работа сервиса остановлена, ожидается исправление...");
                throw new Exception("Корень JSON не является массивом.");
            }

            var rootArray = root.EnumerateArray().ToArray();
            if (rootArray.Length == 0)
            {
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(
                    $"Ошибка в сервисе {cryptocurrencyName}:\n" +
                    "Ответ содержит пустой массив.\n" +
                    $"Полный ответ:\n{response}\n\n" +
                    "Работа сервиса остановлена, ожидается исправление...");
                throw new Exception("Ответ содержит пустой массив, данные свечи не найдены.");
            }
            
            var item = rootArray[0];
            
            if (item.ValueKind != JsonValueKind.Array)
            {
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(
                    $"Ошибка в сервисе {cryptocurrencyName}:\n" +
                    "Первый элемент корневого массива не является массивом.\n" +
                    $"Полный ответ:\n{response}\n\n" +
                    "Работа сервиса остановлена, ожидается исправление...");
                throw new Exception("Первый элемент корневого массива не является массивом.");
            }

            var rowData = item.EnumerateArray().ToArray();
            
            if (rowData.Length < 7)
            {
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(
                    $"Ошибка в сервисе {cryptocurrencyName}:\n" +
                    $"Массив свечи (rowData) содержит {rowData.Length} элементов вместо ожидаемых >= 7.\n" +
                    $"Полный ответ:\n{response}\n\n" +
                    "Работа сервиса остановлена, ожидается исправление...");
                throw new Exception($"rowData содержит {rowData.Length} элементов, а нужно минимум 7.");
            }
            
            var cryptocurrencyDataItem = new CryptocurrencyDataItem
            {
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(rowData[0].GetInt64()).UtcDateTime,
                Open = decimal.Parse(rowData[1].GetString()!, CultureInfo.InvariantCulture),
                High = decimal.Parse(rowData[2].GetString()!, CultureInfo.InvariantCulture),
                Low = decimal.Parse(rowData[3].GetString()!, CultureInfo.InvariantCulture),
                Close = decimal.Parse(rowData[4].GetString()!, CultureInfo.InvariantCulture),
                Volume = decimal.Parse(rowData[5].GetString()!, CultureInfo.InvariantCulture),
                CloseTime = DateTimeOffset.FromUnixTimeMilliseconds(rowData[6].GetInt64()).UtcDateTime,
                Index = index
            };

            return cryptocurrencyDataItem;
        }
        catch (Exception ex)
        {
            await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(
                $"Ошибка при разборе данных в сервисе {cryptocurrencyName}:\n" +
                $"{ex.Message}\n{ex.StackTrace}\n\n" +
                "Сырой ответ:\n" + response + "\n\n" +
                "Работа сервиса остановлена, ожидается исправление...");
            throw;
        }
    }
    
}