using FuturesSignalsBot.Models;

namespace FuturesSignalsBot.Indicators.Smoothing;

public static class VsmaSmoothing
{
    public static void Smooth(List<CryptocurrencyDataItem> values, int period)
    {
        decimal cumulativeVolume = 0;
        decimal cumulativePvPoc = 0;

        for (var i = 0; i < values.Count; i++)
        {
            var item = values[i];
            
            if (!item.VolumeProfileData.TryGetValue(0, out var profileData))
                continue;
            
            if (!profileData.RawPoc.HasValue)
                throw new InvalidOperationException($"RawPoc is null for item with time {item.OpenTime}.");

            cumulativeVolume += item.Volume;
            cumulativePvPoc += profileData.RawPoc.Value * item.Volume;

            if (i >= period)
            {
                var oldItem = values[i - period];
                if (!oldItem.VolumeProfileData.TryGetValue(0, out var oldProfileData))
                    continue;

                if (!oldProfileData.RawPoc.HasValue)
                    throw new InvalidOperationException($"RawPoc is null for oldItem with time {oldItem.OpenTime}.");

                cumulativeVolume -= oldItem.Volume;
                cumulativePvPoc -= oldProfileData.RawPoc.Value * oldItem.Volume;
            }

            profileData.SmoothedPoc = cumulativeVolume != 0 ? cumulativePvPoc / cumulativeVolume : 0;
        }
    }

    public static void SmoothLastItem(List<CryptocurrencyDataItem> values, int period)
    {
        var lastIndex = values.Count - 1;
        var lastItem = values[lastIndex];

        if (!lastItem.VolumeProfileData.TryGetValue(0, out var lastProfileData))
            throw new ArgumentException();
        
        if (!lastProfileData.RawPoc.HasValue)
            throw new InvalidOperationException($"RawPoc is null for item with time {lastItem.OpenTime}.");
        
        var startIndex = Math.Max(0, lastIndex - period + 1);

        decimal cumulativeVolume = 0;
        decimal cumulativePvPoc = 0;

        for (var i = startIndex; i <= lastIndex; i++)
        {
            var item = values[i];
            if (!item.VolumeProfileData.TryGetValue(0, out var profileData))
                continue;
            
            if (!profileData.RawPoc.HasValue)
                throw new InvalidOperationException($"RawPoc is null for oldItem with time {item.OpenTime}.");

            cumulativeVolume += item.Volume;
            cumulativePvPoc += profileData.RawPoc.Value * item.Volume;
            
        }
        
        lastProfileData.SmoothedPoc = cumulativeVolume != 0 ? cumulativePvPoc / cumulativeVolume : 0;
    }

}