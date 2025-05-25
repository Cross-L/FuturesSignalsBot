using System;
using System.Globalization;
using System.Text;

namespace FuturesSignalsBot.Services.Binance;

public static class BinanceHttpHelper
{
    public static string CalculateSignature(string queryString, string secretKey)
    {
        var signatureBytes = Encoding.UTF8.GetBytes(queryString);
        var apiSecretBytes = Encoding.UTF8.GetBytes(secretKey);
        using var hmac = new System.Security.Cryptography.HMACSHA256(apiSecretBytes);
        var hashBytes = hmac.ComputeHash(signatureBytes);
        var signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        return $"&signature={signature}";
    }

    public static DateTimeOffset RoundTimeToInterval(DateTimeOffset time, string interval)
    {
        var (count, type) = ParseInterval(interval);
        return type switch
        {
            'm' =>
                new DateTimeOffset(time.Year, time.Month, time.Day, time.Hour, time.Minute - time.Minute % count, 0,
                    TimeSpan.Zero),
            'h' =>
                new DateTimeOffset(time.Year, time.Month, time.Day, time.Hour - time.Hour % count, 0, 0, TimeSpan.Zero),
            _ => throw new ArgumentException("Неподдерживаемый формат интервала.")
        };
    }

    public static (int count, char type) ParseInterval(string interval)
    {
        var type = interval[^1];
        if (type != 'm' && type != 'h')
            throw new ArgumentException("Интервал должен заканчиваться на 'm' или 'h'.");

        if (!int.TryParse(interval[..^1], out var count))
            throw new ArgumentException("Неверный формат интервала.");

        return (count, type);
    }

    public static DateTimeOffset CalculateStartTime(DateTimeOffset endTime, int count, char type, int limit)
    {
        return type switch
        {
            'm' => endTime.AddMinutes(-count * limit),
            'h' => endTime.AddHours(-count * limit),
            _ => throw new ArgumentException("Неподдерживаемый формат интервала.")
        };
    }
    
    public static int CalculatePrecision(string stepSize)
    {
        var stepDecimal = decimal.Parse(stepSize, CultureInfo.InvariantCulture);
        return BitConverter.GetBytes(decimal.GetBits(stepDecimal)[3])[2];
    }
    
    public static decimal RoundToTickSize(decimal price, decimal tickSize) => Math.Round(price / tickSize) * tickSize;
}