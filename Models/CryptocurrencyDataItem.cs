using System.Collections.Concurrent;

namespace FuturesSignalsBot.Models;

public class CryptocurrencyDataItem
{
    public int Index { get; init; }
    public DateTimeOffset OpenTime { get; init; }
    public DateTimeOffset CloseTime { get; init; }
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }
    public double Tmo180 { get; set; }
    public double Tmo60 { get; set; }
    public double Tmo30 { get; set; }
    public double Tmo240 { get; set; }
    public decimal SmoothedClose { get; set; }
    public decimal Volume { get; init; }
    public (decimal ZScore, decimal InvertedZScore) Score { get; set; }
    public ConcurrentDictionary<decimal,VolumeProfileData> VolumeProfileData { get; } = [];
    
}