using FuturesSignalsBot.Indicators.Smoothing;
using FuturesSignalsBot.Models;

namespace FuturesSignalsBot.Services.Analysis;

public static class VolumeProfileManager
{
    private const int RowSize = 24;
    private const int Period = 90;

    private static readonly (decimal Key, decimal Multiplier)[] Levels =
    [
        (5m, 1.05m), (10m, 1.1m), (20m, 1.2m), 
        (33.3m, 1.333m), (50m, 1.5m),
        (-5m, 0.95m), (-10m, 0.9m), (-20m, 0.8m), 
        (-33.3m, 0.666m), (-50m, 0.5m)
    ];

    public static void InitializeVolumeLevels(TradingDataContainer container)
    {
        var dataToProcess = container.ThirtyMinuteData
            .Skip(container.ThirtyMinuteData.Count - (Period + 200)).ToList();

        foreach (var targetItem in dataToProcess)
        {
            if (!targetItem.VolumeProfileData.TryGetValue(0, out var rawProfileData))
                continue;
            var level0 = rawProfileData.SmoothedPoc;

            foreach (var (key, multiplier) in Levels)
            {
                targetItem.VolumeProfileData.TryAdd(key,
                    new VolumeProfileData(true, null) { SmoothedPoc = level0 * multiplier });
            }
        }
    }

    public static void Update(List<CryptocurrencyDataItem> allData)
    {
        var lastItem = allData.Last();
        if (!lastItem.VolumeProfileData.TryGetValue(0, out var level0))
            throw new ArgumentException();

        foreach (var (key, multiplier) in Levels)
        {
            lastItem.VolumeProfileData.TryAdd(key,
                new VolumeProfileData(true, null)
                    { SmoothedPoc = level0.SmoothedPoc * multiplier });
        }
    }

    public static decimal CalculateCustomVolumeProfileValue(List<CryptocurrencyDataItem> allData, int windowSize,
        decimal volumeArea, int rowSize)
    {
        if (allData.Count >= windowSize)
        {
            var startIndex = allData.Count - windowSize;
            return CalculatePocValue(allData, startIndex, windowSize, volumeArea, rowSize);
        }

        return 0;
    }

    public static void CalculateRawVolumeProfilesForLastItem(TradingDataContainer container,
        VolumeConfiguration configuration)
    {
        var allData = container.ThirtyMinuteData;
        var startIndex = allData.Count - configuration.WindowSize;
        CalculateVolumeProfile(allData, startIndex, configuration.WindowSize);
        VsmaSmoothing.SmoothLastItem(container.ThirtyMinuteData, configuration.Period);
    }

    public static void CalculateRawVolumeProfiles(TradingDataContainer container, VolumeConfiguration configuration)
    {
        var allData = container.ThirtyMinuteData;
        var totalWindows = allData.Count - configuration.WindowSize + 1;
        var requiredCalculations = configuration.Period + 200;
        var startWindow = Math.Max(0, totalWindows - requiredCalculations);
        Parallel.For(startWindow, totalWindows, i => CalculateVolumeProfile(allData, i, configuration.WindowSize));
        VsmaSmoothing.Smooth(allData, configuration.Period);
    }

    private static (decimal RawPoc, decimal RawVaTop, decimal RawVaBottom) ComputeVolumeProfile(
        IReadOnlyList<CryptocurrencyDataItem> allData, int startIndex,
        int windowSize, decimal areaVolumePercent, int rowSize = RowSize)
    {
        var top = decimal.MinValue;
        var bottom = decimal.MaxValue;
        for (var i = 0; i < windowSize; i++)
        {
            var dataPoint = allData[startIndex + i];
            if (dataPoint.High > top) top = dataPoint.High;
            if (dataPoint.Low < bottom) bottom = dataPoint.Low;
        }

        var step = (top - bottom) / rowSize;
        var priceLevels = new decimal[rowSize + 1];
        for (var i = 0; i <= rowSize; i++)
        {
            priceLevels[i] = bottom + step * i;
        }

        var volumeArray = new decimal[rowSize * 2];
        for (var i = 0; i < windowSize; i++)
        {
            var dataPoint = allData[startIndex + i];
            var high = dataPoint.High;
            var low = dataPoint.Low;
            var close = dataPoint.Close;
            var open = dataPoint.Open;
            var volume = dataPoint.Volume;
            var bodyTop = Math.Max(close, open);
            var bodyBottom = Math.Min(close, open);
            var isGreen = close >= open;
            var topWick = high - bodyTop;
            var bottomWick = bodyBottom - low;
            var body = bodyTop - bodyBottom;
            var denominator = 2 * topWick + 2 * bottomWick + body;
            if (denominator == 0)
                continue;
            var bodyVol = body * volume / denominator;
            var topWickVol = 2 * topWick * volume / denominator;
            var bottomWickVol = 2 * bottomWick * volume / denominator;
            for (var j = 0; j < rowSize; j++)
            {
                if (isGreen)
                {
                    volumeArray[j] += GetVolume(priceLevels[j], priceLevels[j + 1], bodyBottom, bodyTop, body, bodyVol);
                }
                else
                {
                    volumeArray[j + rowSize] += GetVolume(priceLevels[j], priceLevels[j + 1], bodyBottom, bodyTop, body,
                        bodyVol);
                }

                var upperVolume = GetVolume(priceLevels[j], priceLevels[j + 1], bodyTop, high, topWick, topWickVol) / 2;
                volumeArray[j] += upperVolume;
                volumeArray[j + rowSize] += upperVolume;
                var lowerVolume = GetVolume(priceLevels[j], priceLevels[j + 1], bodyBottom, low, bottomWick,
                    bottomWickVol) / 2;
                volumeArray[j] += lowerVolume;
                volumeArray[j + rowSize] += lowerVolume;
            }
        }

        var totalVolumes = new decimal[rowSize];
        for (var i = 0; i < rowSize; i++)
        {
            totalVolumes[i] = volumeArray[i] + volumeArray[i + rowSize];
        }

        var maxVolume = decimal.MinValue;
        var pocIndex = 0;
        for (var i = 0; i < rowSize; i++)
        {
            if (totalVolumes[i] > maxVolume)
            {
                maxVolume = totalVolumes[i];
                pocIndex = i;
            }
        }

        var rawData = CalculateVolumeArea(totalVolumes, priceLevels, pocIndex, areaVolumePercent, rowSize);
        return rawData;
    }

    private static void CalculateVolumeProfile(List<CryptocurrencyDataItem> allData, int startIndex, int windowSize)
    {
        var rawData = ComputeVolumeProfile(allData, startIndex, windowSize, 30m);
        var lastDataItem = allData[startIndex + windowSize - 1];
        lastDataItem.VolumeProfileData.TryAdd(0, new VolumeProfileData(true, rawData.RawPoc));
    }

    private static decimal CalculatePocValue(IReadOnlyList<CryptocurrencyDataItem> allData, int startIndex,
        int windowSize, decimal volumeArea, int rowSize)
        => ComputeVolumeProfile(allData, startIndex, windowSize, volumeArea, rowSize).RawPoc;


    private static (decimal RawPoc, decimal RawVaTop, decimal RawVaBottom) CalculateVolumeArea
        (decimal[] totalVolumes, decimal[] levels, int poc, decimal areaVolumePercent, int rowSize = RowSize)
    {
        var totalMax = totalVolumes.Sum() * areaVolumePercent / 100m;
        var vaTotal = totalVolumes[poc];
        var up = poc;
        var down = poc;

        for (var i = 0; i < rowSize - 1; i++)
        {
            if (vaTotal >= totalMax) break;
            var upperVol = up < rowSize - 1 ? totalVolumes[up + 1] : 0;
            var lowerVol = down > 0 ? totalVolumes[down - 1] : 0;
            if (upperVol == 0 && lowerVol == 0) break;
            if (upperVol >= lowerVol)
            {
                vaTotal += upperVol;
                up++;
            }
            else
            {
                vaTotal += lowerVol;
                down--;
            }
        }

        var rawVaTop = levels[up + 1];
        var rawVaBottom = levels[down];
        var rawPoc = (levels[poc] + levels[poc + 1]) / 2;
        return (rawPoc, rawVaTop, rawVaBottom);
    }

    public static LiquidationIntersectResult? CheckLiquidationIntersection30M(CryptocurrencyDataItem item30M)
    {
        var volumeProfileData = item30M.VolumeProfileData
            .Where(d => d.Key != 0 && d.Value.IsForThirtyMinute)
            .ToList();

        foreach (var (key, values) in volumeProfileData)
        {
            if (values is null)
            {
                throw new NullReferenceException(
                    $"Volume manager parameters are null at index: {item30M.Index} Time: {item30M.OpenTime}");
            }

            var value = values.SmoothedPoc;

            if (value > item30M.Low && value < item30M.High)
            {
                return new LiquidationIntersectResult(value, key);
            }
        }

        return null;
    }


    public static LiquidationIntersectResult? CheckLiquidationIntersection5M(CryptocurrencyDataItem item5M,
        CryptocurrencyDataItem item30M)
    {
        var volumeProfileData = item30M.VolumeProfileData
            .Where(d => d.Key != 0 && d.Value.IsForThirtyMinute)
            .ToList();

        foreach (var (key, values) in volumeProfileData)
        {
            if (values is null)
            {
                throw new NullReferenceException(
                    $"Volume manager parameters are null at index: {item30M.Index} Time: {item30M.OpenTime}");
            }

            if (values.SmoothedPoc > item5M.Low && values.SmoothedPoc < item5M.High)
            {
                return new LiquidationIntersectResult(values.SmoothedPoc, key);
            }
        }

        return null;
    }

    public static LiquidationIntersectResult? CheckSpecifiedLiquidationIntersection5M(CryptocurrencyDataItem item5M,
        CryptocurrencyDataItem item30M)
    {
        var volumeProfileData = item30M.VolumeProfileData
            .Where(d => d.Key != 0 && d.Value.IsForThirtyMinute)
            .ToList();

        (decimal start, decimal end) resistance;
        (decimal start, decimal end) support;

        if (item5M.Open > item5M.Close)
        {
            resistance = (item5M.Open, item5M.High);
            support = (item5M.Low, item5M.Close);
        }
        else
        {
            resistance = (item5M.Close, item5M.High);
            support = (item5M.Low, item5M.Open);
        }

        foreach (var (key, values) in volumeProfileData)
        {
            if (values is null)
            {
                throw new NullReferenceException(
                    $"Volume manager parameters are null at index: {item30M.Index} Time: {item30M.OpenTime}");
            }

            var poc = values.SmoothedPoc;
            var inSupportRange = poc > support.start && poc < support.end;
            var inResistanceRange = poc > resistance.start && poc < resistance.end;

            if (inSupportRange || inResistanceRange)
            {
                return new LiquidationIntersectResult(poc, key);
            }
        }

        return null;
    }


    private static decimal GetVolume(decimal y11, decimal y12, decimal y21, decimal y22, decimal height, decimal volume)
        => height == 0
            ? 0
            : Math.Max(
                Math.Min(Math.Max(y11, y12), Math.Max(y21, y22)) - Math.Max(Math.Min(y11, y12), Math.Min(y21, y22)),
                0) * volume / height;
}