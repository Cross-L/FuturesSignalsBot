namespace FuturesSignalsBot.Models.Responses;

public class WalletBalance
{
    public decimal Balance { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal CrossUnPnl { get; set; }
    public string Asset { get; set; } = string.Empty;
    public decimal Equity => Balance + CrossUnPnl;
}
