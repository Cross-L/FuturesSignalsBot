namespace FuturesSignalsBot.Models;

public class VolumeProfileData(bool isForThirtyMinute, decimal? rawPoc)
{
    public bool IsForThirtyMinute { get; } = isForThirtyMinute;
    public decimal? RawPoc { get; } = rawPoc;
    public decimal SmoothedPoc { get; set; }
}
