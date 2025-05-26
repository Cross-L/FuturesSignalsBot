using System.Text.Json.Serialization;

namespace FuturesSignalsBot.Models.Responses;

public class ExchangeInfo
{
    [JsonPropertyName("symbols")] public Dictionary<string, SymbolInfo>? Symbols { get; set; }
}

public class SymbolInfo
{
    [JsonPropertyName("symbol")] public string? Symbol { get; set; }

    [JsonPropertyName("filters")] public List<Filter>? Filters { get; set; }

    // Свойство для быстрого доступа к tickSize
    [JsonIgnore] public string? TickSize => Filters?.FirstOrDefault(f => f.FilterType == "PRICE_FILTER")?.TickSize;
}

public class Filter
{
    [JsonPropertyName("filterType")] public string? FilterType { get; set; }

    [JsonPropertyName("tickSize")] public string? TickSize { get; set; }
}

