namespace FuturesSignalsBot.Models.Responses;

public class UserTrade
{
    public decimal RealizedPnl { get; set; }
    public decimal Commission { get; set; }
    public long Time { get; set; }
    public decimal Qty { get; set; } 
    public decimal Price { get; set; } 
}