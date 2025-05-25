namespace FuturesSignalsBot.Models.IndicatorResults;

public class PreliminaryImpulse(string currency, bool wasIntersection, CryptocurrencyDataItem? intersectionItem, 
    bool isLong, string? liquidationLevel, decimal price, int precision,
    int liquidationLevelNumber, double tmo30)
{
    public string Currency { get; } = currency;
    public CryptocurrencyDataItem? IntersectionItem { get; } = intersectionItem;
    public double Tmo30 { get; } = tmo30;
    public int Precision { get; } = precision;
    public bool IsLong { get; } = isLong;
    public decimal Price { get; } = price;
    public bool WasIntersection { get; } = wasIntersection;
    public string? LiquidationLevel { get; } = liquidationLevel;
    public bool IsMax => LiquidationLevel?.StartsWith('+') == true;
    public int LiquidationLevelNumber { get; } = liquidationLevelNumber;
    public double TmoX3 { get; set; }
    public decimal PocPercentageChange { get; set; }
    public decimal ChangeOv { get; set; }
}