namespace FuturesSignalsBot.Models.Resistance;

public class GeneralResistanceSeries(int index, bool isCorrectCase, CryptocurrencyDataItem firstItem, CryptocurrencyDataItem lastItem,
    CryptocurrencyDataItem firstExtremeItem, CryptocurrencyDataItem secondExtremeItem, CryptocurrencyDataItem correctFirstItem = null,
    CryptocurrencyDataItem correctLastItem = null)
{
    public int Index { get; } = index;
    public bool IsCorrectCase { get; } = isCorrectCase;
    public CryptocurrencyDataItem FirstItem { get; } = firstItem;

    public CryptocurrencyDataItem LastItem { get; } = lastItem;

    public CryptocurrencyDataItem FirstExtremeItem { get; } = firstExtremeItem;

    public CryptocurrencyDataItem SecondExtremeItem { get; } = secondExtremeItem;
    
    public CryptocurrencyDataItem? CorrectFirstItem { get; } = correctFirstItem;
    
    public CryptocurrencyDataItem? CorrectLastItem { get; } = correctLastItem;
}