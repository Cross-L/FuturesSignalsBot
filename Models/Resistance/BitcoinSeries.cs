namespace FuturesSignalsBot.Models.Resistance;

public class BitcoinSeries(bool isLong, List<CryptocurrencyDataItem> items)
{
    public bool IsLong { get; } = isLong;
    public List<CryptocurrencyDataItem> Items { get; } = items;

    public CryptocurrencyDataItem ExtremeItem => IsLong
        ? Items.MinBy(item => item.SmoothedClose)!
        : Items.MaxBy(item => item.SmoothedClose)!;



}