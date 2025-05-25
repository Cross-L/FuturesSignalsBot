namespace FuturesSignalsBot.Models;

public class Cryptocurrency(string name)
{ 
    public string Name { get; } = name;
    public TradingDataContainer TradingDataContainer { get; } = new();
    public int QuantityPrecision { get; set; }
    public double OversoldIndex { get; set; }
    public bool Deactivated { get; set; }
    
}