using Newtonsoft.Json;

namespace FuturesSignalsBot.Models.Responses;

public class Bracket
{
    [JsonProperty("bracket")]
    public int BracketNumber { get; set; }
    
    [JsonProperty("initialLeverage")]
    public decimal InitialLeverage { get; set; }
    
}