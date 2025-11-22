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

            // Validate that the last candle's OpenTime matches
            if (!btcAllData.Last().OpenTime.Equals(allData.Last().OpenTime))
            {
                var message =
                    $"Несоответствие времени последних свечей: BTC OpenTime={btcAllData.Last().OpenTime}, {currencyName} OpenTime={allData.Last().OpenTime}, " +
                    $"Размер btcAllData={btcAllData.Count}, Размер allData={allData.Count}";
                Console.WriteLine(message);
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(message);
                return null;
            }

            // Determine max and min Bitcoin extreme items
            var bitcoinMaxItem =
                firstBitcoinExtremeItem.SmoothedClose > secondBitcoinExtremeItem.SmoothedClose
                    ? firstBitcoinExtremeItem
                    : secondBitcoinExtremeItem;
            var bitcoinMinItem =
                firstBitcoinExtremeItem.SmoothedClose > secondBitcoinExtremeItem.SmoothedClose
                    ? secondBitcoinExtremeItem
                    : firstBitcoinExtremeItem;

            if (firstBitcoinExtremeItem.SmoothedClose == 0 || secondBitcoinExtremeItem.SmoothedClose == 0)
                return null;

            // Get Bitcoin series range based on OpenTime
            DateTimeOffset startTime, endTime;

            if (generalSeries.IsCorrectCase)
            {
                startTime = generalSeries.FirstItem.OpenTime;
                endTime = generalSeries.LastItem.OpenTime;
            }
            else
            {
                startTime = generalSeries.CorrectFirstItem!.OpenTime;
                endTime = generalSeries.CorrectLastItem!.OpenTime;
            }

            // Validate time range
            if (startTime > endTime)
            {
                var message = $"Ошибка диапазона bitcoinSeriesRange: startTime={startTime}, endTime={endTime}, " +
                              $"Размер btcAllData={btcAllData.Count}, Последний OpenTime={btcAllData.Last().OpenTime}";
                Console.WriteLine(message);
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(message);
                return null;
            }

            // Filter btcAllData by OpenTime range
            var bitcoinSeriesRange = btcAllData
                .Where(item => item.OpenTime >= startTime && item.OpenTime <= endTime)
                .OrderBy(item => item.OpenTime)
                .ToList();

            if (bitcoinSeriesRange.Count == 0)
            {
                var message = $"Пустой диапазон bitcoinSeriesRange: startTime={startTime}, endTime={endTime}, " +
                              $"Размер btcAllData={btcAllData.Count}, Последний OpenTime={btcAllData.Last().OpenTime}";
                Console.WriteLine(message);
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(message);
                return null;
            }

            // Get min and max from bitcoin series range
            var (bitcoinMin, bitcoinMax) = GetMinMax(bitcoinSeriesRange);

            CryptocurrencyDataItem firstBitcoinSeriesItem =
                bitcoinMax.OpenTime > bitcoinMin.OpenTime ? bitcoinMin : bitcoinMax;
            CryptocurrencyDataItem secondBitcoinSeriesItem =
                bitcoinMax.OpenTime > bitcoinMin.OpenTime ? bitcoinMax : bitcoinMin;

            if (firstBitcoinSeriesItem.SmoothedClose == 0 || secondBitcoinSeriesItem.SmoothedClose == 0)
                return null;

            // Calculate Bitcoin series and extreme percentage changes
            var bitcoinSeriesChange = CryptoAnalysisTools.CalculatePositivePercentageChange(
                firstBitcoinSeriesItem.SmoothedClose, secondBitcoinSeriesItem.SmoothedClose);

            var bitcoinExtremeChange = CryptoAnalysisTools.CalculatePositivePercentageChange(
                firstBitcoinExtremeItem.SmoothedClose, secondBitcoinExtremeItem.SmoothedClose);

            if (bitcoinSeriesChange == 0) 
                bitcoinSeriesChange = 0.00000001m;
            if (bitcoinExtremeChange == 0) 
                bitcoinExtremeChange = 0.00000001m;

            // Get altcoin series range based on OpenTime
            var altSeriesRange = allData
                .Where(item =>
                    item.OpenTime >= generalSeries.FirstItem.OpenTime &&
                    item.OpenTime <= generalSeries.LastItem.OpenTime)
                .OrderBy(item => item.OpenTime)
                .ToList();

            if (altSeriesRange.Count == 0)
            {
                var message =
                    $"Пустой диапазон altSeriesRange: startTime={generalSeries.FirstItem.OpenTime}, endTime={generalSeries.LastItem.OpenTime}, " +
                    $"Размер allData={allData.Count}, Последний OpenTime={allData.Last().OpenTime}";
                Console.WriteLine(message);
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(message);
                return null;
            }

            // Get min and max from altcoin series range
            var (coinMin, coinMax) = GetMinMax(altSeriesRange);

            var firstItem = coinMin.OpenTime < coinMax.OpenTime ? coinMin : coinMax;
            var lastItem = coinMin.OpenTime < coinMax.OpenTime ? coinMax : coinMin;

            if (firstItem.SmoothedClose == 0 || lastItem.SmoothedClose == 0)        
                return null;
            
            var altSeriesChange = CryptoAnalysisTools.CalculatePositivePercentageChange(
                firstItem.SmoothedClose, lastItem.SmoothedClose);

            if (altSeriesChange == 0)
            {
                var message = $"Изменение альткоина в рамках серии равно 0!\n" +
                              $"Валюта: {currencyName}\n" +
                              $"Серия: {altSeriesRange.First().OpenTime} - {altSeriesRange.Last().OpenTime}\n" +
                              $"Размер allData={allData.Count}, Последний OpenTime={allData.Last().OpenTime}";
                Console.WriteLine(message);
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(message);
            }

            // Get extreme range for altcoin and Bitcoin based on OpenTime
            var extremeStartTime = firstBitcoinExtremeItem.OpenTime;
            var extremeEndTime = secondBitcoinExtremeItem.OpenTime;

            if (extremeStartTime > extremeEndTime)
            {
                var message =
                    $"Ошибка диапазона extremeRange: startTime={extremeStartTime}, endTime={extremeEndTime}, " +
                    $"Размер btcAllData={btcAllData.Count}, Последний OpenTime={btcAllData.Last().OpenTime}";
                Console.WriteLine(message);
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(message);
                return null;
            }

            var extremeAltRange = allData
                .Where(item => item.OpenTime >= extremeStartTime && item.OpenTime <= extremeEndTime)
                .OrderBy(item => item.OpenTime)
                .ToList();

            var extremeBtcRange = btcAllData
                .Where(item => item.OpenTime >= extremeStartTime && item.OpenTime <= extremeEndTime)
                .OrderBy(item => item.OpenTime)
                .ToList();

            if (extremeAltRange.Count == 0 || extremeBtcRange.Count == 0)
            {
                var message =
                    $"Пустой диапазон extremeRange: altRange={extremeAltRange.Count}, btcRange={extremeBtcRange.Count}, " +
                    $"startTime={extremeStartTime}, endTime={extremeEndTime}, " +
                    $"Размер allData={allData.Count}, Размер btcAllData={btcAllData.Count}";
                Console.WriteLine(message);
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(message);
                return null;
            }

            // Get min and max from extreme altcoin range
            var (coinMin1, coinMax1) = GetMinMax(extremeAltRange);

            var firstItem1 = coinMin1.OpenTime < coinMax1.OpenTime ? coinMin1 : coinMax1;
            var lastItem1 = coinMin1.OpenTime < coinMax1.OpenTime ? coinMax1 : coinMin1;

            if (firstItem1.SmoothedClose == 0 || lastItem1.SmoothedClose == 0) 
                return null;

            var extremeCoinPercentageChange = CryptoAnalysisTools.CalculatePositivePercentageChange(
                firstItem1.SmoothedClose, lastItem1.SmoothedClose);

            // Determine trend directions
            var isBtcExtremeIncreased = firstBitcoinExtremeItem.SmoothedClose < secondBitcoinExtremeItem.SmoothedClose;
            var isBtcSeriesIncreased = firstBitcoinSeriesItem.SmoothedClose < secondBitcoinSeriesItem.SmoothedClose;
            var isAltCoinSeriesIncreased = firstItem.SmoothedClose < lastItem.SmoothedClose;
            var isAltCoinMinMaxIncreased = firstItem1.SmoothedClose < lastItem1.SmoothedClose;

            // Create result
            var basicInfo = new BasicResistanceInfo(
                generalSeries.Index, currencyName, generalSeries.FirstItem, generalSeries.LastItem,
                isBtcSeriesIncreased, isBtcExtremeIncreased, isAltCoinMinMaxIncreased, isAltCoinSeriesIncreased,
                extremeCoinPercentageChange, bitcoinSeriesChange, bitcoinExtremeChange,
                altSeriesChange, bitcoinMinItem, bitcoinMaxItem, firstItem1, lastItem1,
                extremeAltRange, extremeBtcRange);

            return basicInfo;
        }
        catch (DivideByZeroException divEx)
        {
            Console.WriteLine($"[CRITICAL] DivideByZero in ProcessOppositeTrend for {currencyName}: {divEx.StackTrace}");
            return null;
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
        if (coinMax == null || coinMin == null)
            return (selectedCandles.First(), selectedCandles.Last());
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

            if (targetImpulses.Count == 0)
                continue;

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


        TopDelays = [.. TopDelays
            .OrderByDescending(info => info.targetImpulses)
            .Take(20)
            .OrderByDescending(info => info.delay)];

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

        MaxExtremeTop = [.. combinedTop
            .OrderByDescending(info => info.ExtremeMultiplier)
            .Take(5)];

        MinExtremeTop = [.. combinedTop
            .OrderBy(info => info.ExtremeMultiplier)
            .Take(5)];

        MaxSeriesTop = [.. combinedTop
            .OrderByDescending(info => info.SeriesMultiplier)
            .Take(5)];

        AntiExtremeTop = [.. AntiExtremeTop
            .OrderByDescending(info => info.ExtremeMultiplier)
            .Take(10)];
    }
}