using System;
using System.Collections.Concurrent;
using FuturesSignalsBot.Models.IndicatorResults;

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
    public double TmoX3 => (Tmo30 + Tmo60 + Tmo180) / 3;
    public decimal SmoothedClose { get; set; }
    public decimal Volume { get; init; }
    public BandwidthResult Bandwidth { get; set; }
    public LinearRegression Tf1Regression { get; set; }
    public LinearRegression Tf3Regression { get; set; }
    public LinearRegression Tf5Regression { get; set; }
    public decimal CustomVolumeProfileValue { get; set; }
    public FramaResult? Frama { get; set; }
    public ConcurrentDictionary<decimal,VolumeProfileData> VolumeProfileData { get; } = [];
    
}