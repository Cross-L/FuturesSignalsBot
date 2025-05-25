using Newtonsoft.Json;

namespace FuturesSignalsBot.Models.Responses;

public class PositionRisk
{
    [JsonProperty("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonProperty("leverage")]
    public decimal Leverage { get; set; }
    
}