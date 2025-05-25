using System.Collections.Generic;
using FuturesSignalsBot.Models;
using FuturesSignalsBot.Models.Resistance;

namespace FuturesSignalsBot.Services.Analysis;

public static class BitcoinResistanceService
{
    public static List<GeneralResistanceSeries> ResistanceSeries { get; set; } = [];
    public static List<CryptocurrencyDataItem> CandlesData { get; set; } = [];

}