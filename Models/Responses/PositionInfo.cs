using Newtonsoft.Json;

namespace FuturesSignalsBot.Models.Responses;

public class PositionInfo
{
    [JsonProperty("symbol")]
    public string Symbol { get; set; } = string.Empty;
    
    [JsonProperty("positionAmt")]
    public decimal PositionAmt { get; set; }
    
    [JsonProperty("entryPrice")]
    public decimal EntryPrice { get; set; }
}
