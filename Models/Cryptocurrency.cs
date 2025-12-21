using FuturesSignalsBot.Enums;

namespace FuturesSignalsBot.Models;

public class Cryptocurrency(string name)
{ 
    public string Name { get; } = name;
    public TradingDataContainer TradingDataContainer { get; } = new();
    public int QuantityPrecision { get; set; }
    public double OversoldIndex { get; set; }
    public bool Deactivated => DeactivationReason != CurrencyDeactivationReason.None;
    public CurrencyDeactivationReason DeactivationReason { get; set; } = CurrencyDeactivationReason.None;
    public int? Top24hRank { get; set; }
    public decimal FundingRate { get; set; }
    public decimal PreviousFundingRate { get; set; }
    public DateTimeOffset? FundingReversalTime { get; set; }
}