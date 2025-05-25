namespace FuturesSignalsBot.Models;

public class VolumeConfiguration(int period, int windowSize)
{
    public int Period { get; } = period;
    public int WindowSize { get; } = windowSize;
}
