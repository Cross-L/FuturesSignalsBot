using FuturesSignalsBot.Core;
using FuturesSignalsBot.Models;
using FuturesSignalsBot.Models.Resistance;

namespace FuturesSignalsBot.Services.Analysis;

public static class CorrelationTrendAnalyzer
{
    public static List<BasicResistanceInfo> MaxExtremeTop { get; private set; } = [];

    public static List<BasicResistanceInfo> MaxSeriesTop { get; private set; } = [];

    public static List<BasicResistanceInfo> MinExtremeTop { get; private set; } = [];

    public static List<BasicResistanceInfo> AntiExtremeTop { get; private set; } = [];

    public static List<(string name, decimal delay, int targetImpulses)> TopDelays { get; private set; } = [];


    public static async Task<BasicResistanceInfo?> ProcessOppositeTrend(string currencyName,
        List<CryptocurrencyDataItem> btcAllData, List<CryptocurrencyDataItem> allData,
        GeneralResistanceSeries generalSeries)
    {
        try
        {
            var firstBitcoinExtremeItem = generalSeries.FirstExtremeItem;
            var secondBitcoinExtremeItem = generalSeries.SecondExtremeItem;

            CryptocurrencyDataItem bitcoinMaxItem;
            CryptocurrencyDataItem bitcoinMinItem;

            if (!btcAllData.Last().OpenTime.Equals(allData.Last().OpenTime))
            {
                var message =
                    $"Несоответствие времени последних свечей: BTC OpenTime={btcAllData.Last().OpenTime}, {currencyName} OpenTime={allData.Last().OpenTime}, " +
                    $"Размер btcAllData={btcAllData.Count}, Размер allData={allData.Count}";
                Console.WriteLine(message);
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(message);
                return null;
            }

            if (firstBitcoinExtremeItem.SmoothedClose > secondBitcoinExtremeItem.SmoothedClose)
            {
                bitcoinMaxItem = firstBitcoinExtremeItem;
                bitcoinMinItem = secondBitcoinExtremeItem;
            }
            else
            {
                bitcoinMaxItem = secondBitcoinExtremeItem;
                bitcoinMinItem = firstBitcoinExtremeItem;
            }

            List<CryptocurrencyDataItem> bitcoinSeriesRange;

            if (generalSeries.IsCorrectCase)
            {
                var startIndex = generalSeries.FirstItem.Index;
                var count = generalSeries.LastItem.Index - startIndex + 1;
                if (startIndex < 0 || count <= 0 || startIndex + count > btcAllData.Count)
                {
                    var message = $"Ошибка диапазона bitcoinSeriesRange: startIndex={startIndex}, count={count}, " +
                                  $"Размер btcAllData={btcAllData.Count}, Последний OpenTime={btcAllData.Last().OpenTime}";
                    Console.WriteLine(message);
                    await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(message);
                    return null;
                }

                bitcoinSeriesRange = btcAllData.GetRange(startIndex, count);
            }
            else
            {
                var startIndex = generalSeries.CorrectFirstItem!.Index;
                var count = generalSeries.CorrectLastItem!.Index - startIndex + 1;
                if (startIndex < 0 || count <= 0 || startIndex + count > btcAllData.Count)
                {
                    var message =
                        $"Ошибка диапазона bitcoinSeriesRange (CorrectCase): startIndex={startIndex}, count={count}, " +
                        $"Размер btcAllData={btcAllData.Count}, Последний OpenTime={btcAllData.Last().OpenTime}";
                    Console.WriteLine(message);
                    await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(message);
                    return null;
                }

                bitcoinSeriesRange = btcAllData.GetRange(startIndex, count);
            }

            var (bitcoinMin, bitcoinMax) = GetMinMax(bitcoinSeriesRange);

            CryptocurrencyDataItem firstBitcoinSeriesItem;
            CryptocurrencyDataItem secondBitcoinSeriesItem;

            if (bitcoinMax.Index > bitcoinMin.Index)
            {
                firstBitcoinSeriesItem = bitcoinMin;
                secondBitcoinSeriesItem = bitcoinMax;
            }
            else
            {
                firstBitcoinSeriesItem = bitcoinMax;
                secondBitcoinSeriesItem = bitcoinMin;
            }

            var bitcoinSeriesChange = CryptoAnalysisTools.CalculatePositivePercentageChange
                (firstBitcoinSeriesItem.SmoothedClose, secondBitcoinSeriesItem.SmoothedClose);

            var bitcoinExtremeChange = CryptoAnalysisTools.CalculatePositivePercentageChange
                (firstBitcoinExtremeItem.SmoothedClose, secondBitcoinExtremeItem.SmoothedClose);

            var altStartIndex = generalSeries.FirstItem.Index;
            var altCount = generalSeries.LastItem.Index - altStartIndex + 1;
            if (altStartIndex < 0 || altCount <= 0 || altStartIndex + altCount > allData.Count)
            {
                var message = $"Ошибка диапазона altSeriesRange: startIndex={altStartIndex}, count={altCount}, " +
                              $"Размер allData={allData.Count}, Последний OpenTime={allData.Last().OpenTime}";
                Console.WriteLine(message);
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(message);
                return null;
            }

            var altSeriesRange = allData.GetRange(altStartIndex, altCount);

            var (coinMin, coinMax) = GetMinMax(altSeriesRange);

            var firstItem = coinMin.Index < coinMax.Index ? coinMin : coinMax;
            var lastItem = coinMin.Index < coinMax.Index ? coinMax : coinMin;

            var altSeriesChange = coinMax.Index > coinMin.Index
                ? CryptoAnalysisTools.CalculatePositivePercentageChange(coinMin.SmoothedClose, coinMax.SmoothedClose)
                : CryptoAnalysisTools.CalculatePositivePercentageChange(coinMax.SmoothedClose, coinMin.SmoothedClose);

            if (altSeriesChange == 0)
            {
                var message = $"Изменение альткоина в рамках серии равно 0!\n" +
                              $"Валюта: {currencyName}\n" +
                              $"Серия: {altSeriesRange.First().OpenTime} - {altSeriesRange.Last().OpenTime}\n" +
                              $"Размер allData={allData.Count}, Последний OpenTime={allData.Last().OpenTime}";
                Console.WriteLine(message);
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(message);
            }

            var extremeStartIndex = firstBitcoinExtremeItem.Index;
            var extremeCount = secondBitcoinExtremeItem.Index - extremeStartIndex + 1;
            if (extremeStartIndex < 0 || extremeCount <= 0 || extremeStartIndex + extremeCount > allData.Count)
            {
                var message =
                    $"Ошибка диапазона extremeAltRange: startIndex={extremeStartIndex}, count={extremeCount}, " +
                    $"Размер allData={allData.Count}, Последний OpenTime={allData.Last().OpenTime}";
                Console.WriteLine(message);
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(message);
                return null;
            }

            var extremeAltRange = allData.GetRange(extremeStartIndex, extremeCount);

            if (extremeStartIndex < 0 || extremeCount <= 0 || extremeStartIndex + extremeCount > btcAllData.Count)
            {
                var message =
                    $"Ошибка диапазона extremeBtcRange: startIndex={extremeStartIndex}, count={extremeCount}, " +
                    $"Размер btcAllData={btcAllData.Count}, Последний OpenTime={btcAllData.Last().OpenTime}";
                Console.WriteLine(message);
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(message);
                return null;
            }

            var extremeBtcRange = btcAllData.GetRange(extremeStartIndex, extremeCount);

            var (coinMin1, coinMax1) = GetMinMax(extremeAltRange);

            var firstItem1 = coinMin1.Index < coinMax1.Index ? coinMin1 : coinMax1;
            var lastItem1 = coinMin1.Index < coinMax1.Index ? coinMax1 : coinMin1;

            var extremeCoinPercentageChange =
                CryptoAnalysisTools.CalculatePositivePercentageChange(firstItem1.SmoothedClose,
                    lastItem1.SmoothedClose);

            var isBtcExtremeIncreased = firstBitcoinExtremeItem.SmoothedClose < secondBitcoinExtremeItem.SmoothedClose;
            var isBtcSeriesIncreased = firstBitcoinSeriesItem.SmoothedClose < secondBitcoinSeriesItem.SmoothedClose;

            var isAltCoinSeriesIncreased = firstItem.SmoothedClose < lastItem.SmoothedClose;
            var isAltCoinMinMaxIncreased = firstItem1.SmoothedClose < lastItem1.SmoothedClose;

            var basicInfo = new BasicResistanceInfo(generalSeries.Index, currencyName, generalSeries.FirstItem,
                generalSeries.LastItem,
                isBtcSeriesIncreased, isBtcExtremeIncreased, isAltCoinMinMaxIncreased, isAltCoinSeriesIncreased,
                extremeCoinPercentageChange, bitcoinSeriesChange, bitcoinExtremeChange,
                altSeriesChange, bitcoinMinItem, bitcoinMaxItem, firstItem1,
                lastItem1, extremeAltRange, extremeBtcRange);

            return basicInfo;
        }
        catch (Exception ex)
        {
            var message = $"Ошибка в ProcessOppositeTrend для {currencyName}: {ex.GetType().Name} - {ex.Message}\n" +
                          $"Размер btcAllData={btcAllData.Count}, Последний OpenTime={btcAllData.Last().OpenTime}\n" +
                          $"Размер allData={allData.Count}, Последний OpenTime={allData.Last().OpenTime}";
            Console.WriteLine(message);
            await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(message);
            return null;
        }
    }


    private static (CryptocurrencyDataItem, CryptocurrencyDataItem) GetMinMax(
        List<CryptocurrencyDataItem> selectedCandles)
    {
        var coinMax = selectedCandles.MaxBy(item => item.SmoothedClose);
        var coinMin = selectedCandles.MinBy(item => item.SmoothedClose);
        return (coinMin, coinMax)!;
    }

    public static List<GeneralResistanceSeries> CalculateBitcoinResistance(List<CryptocurrencyDataItem> bitcoinData)
    {
        var longSeries = new List<BitcoinSeries>();
        var shortSeries = new List<BitcoinSeries>();
        var currentLongSeriesItems = new List<CryptocurrencyDataItem>();
        var currentShortSeriesItems = new List<CryptocurrencyDataItem>();

        for (var i = 49; i < bitcoinData.Count; i++)
        {
            var currentItem = bitcoinData[i];
            var startIndex = i - 49;
            var last50Elements = bitcoinData.GetRange(startIndex, 50);
            currentItem.Tmo240 = CryptoAnalysisTools.CalculateLastTmoForTimeFrame(last50Elements, 30);

            if (currentItem.Tmo240 > 7.00)
            {
                currentShortSeriesItems.Add(currentItem);

                if (currentLongSeriesItems.Count >= 2)
                {
                    var newSeries = new BitcoinSeries(true, [..currentLongSeriesItems]);
                    longSeries.Add(newSeries);
                    currentLongSeriesItems.Clear();
                }
            }
            else if (currentItem.Tmo240 < -7.00)
            {
                currentLongSeriesItems.Add(currentItem);

                if (currentShortSeriesItems.Count >= 2)
                {
                    var newSeries = new BitcoinSeries(false, [..currentShortSeriesItems]);
                    shortSeries.Add(newSeries);
                    currentShortSeriesItems.Clear();
                }
            }
            else
            {
                if (currentShortSeriesItems.Count >= 2)
                {
                    var newSeries = new BitcoinSeries(false, [..currentShortSeriesItems]);
                    shortSeries.Add(newSeries);
                    currentShortSeriesItems.Clear();
                }
                else
                {
                    currentShortSeriesItems.Clear();
                }

                if (currentLongSeriesItems.Count >= 2)
                {
                    var newSeries = new BitcoinSeries(true, [..currentLongSeriesItems]);
                    longSeries.Add(newSeries);
                    currentLongSeriesItems.Clear();
                }
                else
                {
                    currentLongSeriesItems.Clear();
                }
            }
        }

        if (currentLongSeriesItems.Count >= 2)
        {
            var newSeries = new BitcoinSeries(true, [..currentLongSeriesItems]);
            longSeries.Add(newSeries);
        }

        if (currentShortSeriesItems.Count >= 2)
        {
            var newSeries = new BitcoinSeries(false, [..currentShortSeriesItems]);
            shortSeries.Add(newSeries);
        }

        var bitcoinSeries = longSeries.Concat(shortSeries).OrderBy(series => series.Items.Last().OpenTime).ToList();

        var allGeneralSeries = new List<GeneralResistanceSeries>();
        var index = 0;

        for (var i = 0; i < bitcoinSeries.Count - 1; i++)
        {
            var current = bitcoinSeries[i];
            var next = bitcoinSeries[i + 1];

            if (current.IsLong == next.IsLong)
            {
                var doubledSeries = DoubleSeries(current, next, bitcoinData);
                var firstSeries = new GeneralResistanceSeries(index, false, doubledSeries.firstSeries.Items.First(),
                    doubledSeries.firstSeries.Items.Last(), doubledSeries.firstSeries.ExtremeItem,
                    doubledSeries.firstSeries.Items.Last(), current.Items.First(),
                    doubledSeries.firstSeries.Items.Last());

                var secondSeries = new GeneralResistanceSeries(++index, false, doubledSeries.secondSeries.Items.First(),
                    doubledSeries.secondSeries.Items.Last(), doubledSeries.secondSeries.ExtremeItem,
                    doubledSeries.secondSeries.Items.Last(), doubledSeries.firstSeries.Items.Last(), next.Items.Last());

                allGeneralSeries.Add(firstSeries);
                allGeneralSeries.Add(secondSeries);
            }
            else
            {
                var generalSeries = new GeneralResistanceSeries(index, true, current.Items.First(), next.Items.Last(),
                    current.ExtremeItem, next.ExtremeItem);
                allGeneralSeries.Add(generalSeries);
            }
        }

        return allGeneralSeries;
    }

    private static (BitcoinSeries firstSeries, BitcoinSeries secondSeries) DoubleSeries(BitcoinSeries current,
        BitcoinSeries next, List<CryptocurrencyDataItem> btcAllData)
    {
        var seriesItem = current.ExtremeItem;
        var nextSeriesItem = next.ExtremeItem;

        var betweenRange = btcAllData.GetRange(seriesItem.Index, nextSeriesItem.Index - seriesItem.Index + 1);

        var mediateItem = !current.IsLong
            ? betweenRange.MinBy(item => item.SmoothedClose)!
            : betweenRange.MaxBy(item => item.SmoothedClose)!;

        var firstSeriesItems = btcAllData.GetRange
            (seriesItem.Index, mediateItem.Index - seriesItem.Index + 1);
        var secondSeriesItems = btcAllData.GetRange
            (mediateItem.Index, nextSeriesItem.Index - mediateItem.Index + 1);

        var firstSeries = new BitcoinSeries(current.IsLong, firstSeriesItems);
        var secondSeries = new BitcoinSeries(!next.IsLong, secondSeriesItems);

        return (firstSeries, secondSeries);
    }

    public static void CalculateCorrelationTrends(
        List<List<BasicResistanceInfo>> allResistanceInfos)
    {
        TopDelays = [];
        MaxExtremeTop = [];
        MinExtremeTop = [];
        AntiExtremeTop = [];
        MaxSeriesTop = [];

        var lastInfos = allResistanceInfos
            .Where(sublist => sublist is { Count: > 0 })
            .Select(sublist => sublist.Last())
            .ToList();

        foreach (var resistanceInfos in allResistanceInfos)
        {
            var targetImpulses = resistanceInfos
                .Where(info => info.IsBtcExtremeIncreased == info.IsAltCoinExtremeIncreased)
                .Where(info =>
                {
                    var firstBitcoinExtremum = info.BtcMinItem.Index > info.BtcMaxItem.Index
                        ? info.BtcMaxItem
                        : info.BtcMinItem;
                    return firstBitcoinExtremum.Index != info.AltFirstExtremum.Index;
                })
                .ToList();

            var total = targetImpulses.Sum(info =>
            {
                var firstBitcoinExtremum = info.BtcMinItem.Index > info.BtcMaxItem.Index
                    ? info.BtcMaxItem
                    : info.BtcMinItem;
                return Math.Abs(info.AltFirstExtremum.Index - firstBitcoinExtremum.Index);
            });

            var altCoinName = resistanceInfos.First().AltCoinName;
            var delayMultiplier = (decimal)total / targetImpulses.Count;
            TopDelays.Add((altCoinName, delayMultiplier, targetImpulses.Count));
        }


        TopDelays = TopDelays
            .OrderByDescending(info => info.targetImpulses)
            .Take(20)
            .OrderByDescending(info => info.delay)
            .ToList();

        var combinedTop = new List<BasicResistanceInfo>();

        foreach (var info in lastInfos)
        {
            var isSeriesIncreasedEqual = info.IsBtcSeriesIncreased == info.IsAltCoinSeriesIncreased;
            var isExtremeIncreasedEqual = info.IsBtcExtremeIncreased == info.IsAltCoinExtremeIncreased;

            if (isSeriesIncreasedEqual && isExtremeIncreasedEqual)
            {
                combinedTop.Add(info);
            }
            else if (isSeriesIncreasedEqual && !isExtremeIncreasedEqual)
            {
                AntiExtremeTop.Add(info);
            }
        }

        MaxExtremeTop = combinedTop
            .OrderByDescending(info => info.ExtremeMultiplier)
            .Take(5)
            .ToList();

        MinExtremeTop = combinedTop
            .OrderBy(info => info.ExtremeMultiplier)
            .Take(5)
            .ToList();

        MaxSeriesTop = combinedTop
            .OrderByDescending(info => info.SeriesMultiplier)
            .Take(5)
            .ToList();

        AntiExtremeTop = AntiExtremeTop
            .OrderByDescending(info => info.ExtremeMultiplier)
            .Take(10)
            .ToList();
    }
}