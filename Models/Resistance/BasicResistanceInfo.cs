using System.Collections.Generic;

namespace FuturesSignalsBot.Models.Resistance;

public class BasicResistanceInfo(
    int index,
    string altCoinName,
    CryptocurrencyDataItem seriesStart,
    CryptocurrencyDataItem seriesEnd,
    bool isBtcSeriesIncreased,
    bool isBtcExtremeIncreased,
    bool isAltCoinExtremeIncreased,
    bool isAltCoinSeriesIncreased,
    decimal extremeAltCoinChange,
    decimal seriesBtcChange,
    decimal bitcoinExtremeChange,
    decimal seriesAltCoinChange,
    CryptocurrencyDataItem btcMinItem,
    CryptocurrencyDataItem btcMaxItem,
    CryptocurrencyDataItem altFirstExtremum,
    CryptocurrencyDataItem altSecondExtremum,
    List<CryptocurrencyDataItem> altExtremeRange,
    List<CryptocurrencyDataItem> btcExtremeRange)
{
    public int Index { get; } = index;
    public string AltCoinName { get; } = altCoinName;
    public bool IsBtcSeriesIncreased { get; } = isBtcSeriesIncreased;
    public bool IsBtcExtremeIncreased { get; } = isBtcExtremeIncreased;
    public bool IsAltCoinExtremeIncreased { get; } = isAltCoinExtremeIncreased;
    public bool IsAltCoinSeriesIncreased { get; } = isAltCoinSeriesIncreased;
    public decimal SeriesBtcChange { get; } = seriesBtcChange;
    public decimal ExtremeBtcChange { get; } = bitcoinExtremeChange;

    public decimal SeriesMultiplier => SeriesAltCoinChange / SeriesBtcChange;

    public decimal ExtremeMultiplier => ExtremeAltCoinChange / ExtremeBtcChange;
    public decimal ExtremeAltCoinChange { get; } = extremeAltCoinChange;
    public decimal SeriesAltCoinChange { get; } = seriesAltCoinChange;
    public CryptocurrencyDataItem BtcMinItem { get; } = btcMinItem;
    public CryptocurrencyDataItem BtcMaxItem { get; } = btcMaxItem;
    public CryptocurrencyDataItem SeriesStart { get; } = seriesStart;
    public CryptocurrencyDataItem SeriesEnd { get; } = seriesEnd;

    public CryptocurrencyDataItem AltFirstExtremum { get; } = altFirstExtremum;

    public CryptocurrencyDataItem AltSecondExtremum { get; } = altSecondExtremum;

    public List<CryptocurrencyDataItem> AltExtremeRange { get; } = altExtremeRange;

    public List<CryptocurrencyDataItem> BtcExtremeRange { get; } = btcExtremeRange;

    public decimal AltLoi { get; set; }

    public decimal BtcLoi { get; set; }
}