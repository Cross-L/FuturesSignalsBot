namespace FuturesSignalsBot.Models.IndicatorResults;

public class FramaResult(bool isRed, bool isBlue)
{
    public bool IsRed { get; } = isRed;
    public bool IsBlue { get; } = isBlue;
}