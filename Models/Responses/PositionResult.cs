namespace FuturesSignalsBot.Models.Responses;

public class PositionResult
{
    public bool IsError { get; set; }
    public string? ErrorMessage { get; set; }
    public PositionInfo? PositionInfo { get; set; }
}