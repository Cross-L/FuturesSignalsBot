using System.Collections.Generic;
using System.Linq;
using FuturesSignalsBot.Models.IndicatorResults;

namespace FuturesSignalsBot.Services.Analysis;

public static class PreliminaryImpulseAnalyzer
{
    public static List<PreliminaryImpulse> TopByHigh { get; private set; } = [];
    public static List<PreliminaryImpulse> TopByLow { get; private set; } = [];
    

    public static void UpdateTopLists(List<PreliminaryImpulse?> preliminaryImpulses5M)
    {
        TopByHigh = GetTopImpulsesByLowHigh(preliminaryImpulses5M, 15, false);
        TopByLow = GetTopImpulsesByLowHigh(preliminaryImpulses5M, 15, true);
    }

    private static List<PreliminaryImpulse> GetTopImpulsesByLowHigh(
        IEnumerable<PreliminaryImpulse?> preliminaryImpulses, int topCount, bool byLow)
    {
        var nonNullImpulses = preliminaryImpulses.OfType<PreliminaryImpulse>();
        var sortedImpulses = byLow
            ? nonNullImpulses.OrderBy(impulse => impulse.ChangeOv).Take(topCount).ToList()
            : nonNullImpulses.OrderByDescending(impulse => impulse.ChangeOv).Take(topCount).ToList();
            
        var btcImpulse = sortedImpulses.FirstOrDefault(impulse => impulse.Currency == "BTCUSDT");

        if (btcImpulse != null)
        {
            sortedImpulses.Remove(btcImpulse);
            sortedImpulses.Insert(0, btcImpulse);
        }

        return sortedImpulses;
    }
}