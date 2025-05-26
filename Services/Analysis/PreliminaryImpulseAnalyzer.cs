using FuturesSignalsBot.Models.IndicatorResults;

namespace FuturesSignalsBot.Services.Analysis;

public static class PreliminaryImpulseAnalyzer
{
    public static List<PreliminaryImpulse> TopByHigh { get; private set; } = [];
    public static List<PreliminaryImpulse> TopByLow { get; private set; } = [];
    
    public static List<PreliminaryImpulse> TopShortLiquidations { get; private set; } = [];
    public static List<PreliminaryImpulse> TopLongLiquidations { get; private set; } = [];
    
    public static void UpdateTopLists(List<PreliminaryImpulse?> preliminaryImpulses5M)
    {
        TopByHigh = GetTopImpulsesByLowHigh(preliminaryImpulses5M, 15, false);
        TopByLow = GetTopImpulsesByLowHigh(preliminaryImpulses5M, 15, true);
        TopLongLiquidations = GetTopLiquidationImpulses(preliminaryImpulses5M, 10, true);
        TopShortLiquidations = GetTopLiquidationImpulses(preliminaryImpulses5M, 10, false);
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

    private static List<PreliminaryImpulse> GetTopLiquidationImpulses(
        IEnumerable<PreliminaryImpulse?> preliminaryImpulses, int topCount, bool isLong)
    {
        var nonNullImpulses = preliminaryImpulses.OfType<PreliminaryImpulse>();
        var sortedImpulses = nonNullImpulses
            .Where(i => i.IsLong != isLong && i.WasIntersection)
            .OrderByDescending(impulse => impulse.LiquidationLevelNumber).Take(topCount).ToList();
        return sortedImpulses;
    }
}