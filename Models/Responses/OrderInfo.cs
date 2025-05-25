using Newtonsoft.Json;

namespace FuturesSignalsBot.Models.Responses;

public class OrderInfo
{
    [JsonProperty("orderId")]
    public long OrderId { get; set; }
    
    [JsonProperty("symbol")]
    public string Symbol { get; set; } = string.Empty;
    
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;
}