using FuturesSignalsBot.Models.IndicatorResults;

namespace FuturesSignalsBot.Services.Analysis;

public static class PreliminaryImpulseAnalyzer
{
    public static List<PreliminaryImpulse> TopByHigh { get; private set; } = [];
    public static List<PreliminaryImpulse> TopByLow { get; private set; } = [];
    
    public static List<PreliminaryImpulse> TopShortLiquidations { get; private set; } = [];
    public static List<PreliminaryImpulse> TopLongLiquidations { get; private set; } = [];
    
    public static List<PreliminaryImpulse> TopShortZScore { get; private set; } = [];
    public static List<PreliminaryImpulse> TopLongZScore { get; private set; } = [];
    
    public static List<PreliminaryImpulse> TopShortZScorePercentage { get; private set; } = [];
    public static List<PreliminaryImpulse> TopLongZScorePercentage { get; private set; } = [];
    
    public static List<PreliminaryImpulse> TopShortOpenMin{ get; private set; } = [];
    public static List<PreliminaryImpulse> TopLongOpenMax { get; private set; } = [];
    
    public static List<PreliminaryImpulse> BestShorts{ get; private set; } = [];
    public static List<PreliminaryImpulse> BestLongs { get; private set; } = [];
    
    public static void UpdateTopLists(List<PreliminaryImpulse?> preliminaryImpulses5M)
    {
        TopByHigh = GetTopImpulsesByLowHigh(preliminaryImpulses5M, 15, false);
        TopByLow = GetTopImpulsesByLowHigh(preliminaryImpulses5M, 15, true);
        
        TopLongLiquidations = GetTopLiquidationImpulses(preliminaryImpulses5M, 10, true);
        TopShortLiquidations = GetTopLiquidationImpulses(preliminaryImpulses5M, 10, false);
        
        TopLongZScore = GetTopZScoreImpulses(preliminaryImpulses5M, 10, true);
        TopShortZScore = GetTopZScoreImpulses(preliminaryImpulses5M, 10, false);
        
        TopLongZScorePercentage = GetTopZScorePercentageImpulses(preliminaryImpulses5M, 10, true);
        TopShortZScorePercentage = GetTopZScorePercentageImpulses(preliminaryImpulses5M, 10, false);
        
        TopLongOpenMax = GetTopMinMaxImpulses(preliminaryImpulses5M, 10, true);
        TopShortOpenMin = GetTopMinMaxImpulses(preliminaryImpulses5M, 10, false);
        
        BestShorts = GetBestImpulses(preliminaryImpulses5M, 10, false);
        BestLongs = GetBestImpulses(preliminaryImpulses5M, 10, true);
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
    
    private static List<PreliminaryImpulse> GetTopZScoreImpulses(
        IEnumerable<PreliminaryImpulse?> preliminaryImpulses, int topCount, bool isLong)
    {
        var nonNullImpulses = preliminaryImpulses.OfType<PreliminaryImpulse>();
        var sortedImpulses = nonNullImpulses
            .Where(i => i.IsLong != isLong && 
                        (!i.WasIntersection || 
                         (isLong && i.LiquidationLevel!.StartsWith("-")) || 
                         (!isLong && !i.LiquidationLevel!.StartsWith("-"))))
            .OrderByDescending(impulse => Math.Abs(impulse.Score.ZScore))
            .Take(topCount)
            .ToList();
        return sortedImpulses;
    }
    
    private static List<PreliminaryImpulse> GetTopZScorePercentageImpulses(
        IEnumerable<PreliminaryImpulse?> preliminaryImpulses, int topCount, bool isLong)
    {
        var nonNullImpulses = preliminaryImpulses.OfType<PreliminaryImpulse>();
        var sortedImpulses = nonNullImpulses
            .Where(i => i.IsLong != isLong)
            .OrderByDescending(impulse => impulse.ZScoreRatio)
            .Take(topCount)
            .ToList();
        return sortedImpulses;
    }
    
    private static List<PreliminaryImpulse> GetTopMinMaxImpulses(
        IEnumerable<PreliminaryImpulse?> preliminaryImpulses, int topCount, bool isLong)
    {
        var nonNullImpulses = preliminaryImpulses.OfType<PreliminaryImpulse>();
        var sortedImpulses = nonNullImpulses
            .Where(i => i.IsLong != isLong)
            .OrderByDescending(impulse => impulse.MinMaxPercentage)
            .Take(topCount)
            .ToList();
        return sortedImpulses;
    }
    
    private static List<PreliminaryImpulse> GetBestImpulses(
        IEnumerable<PreliminaryImpulse?> preliminaryImpulses, int topCount, bool isLong)
    {
        var nonNullImpulses = preliminaryImpulses.OfType<PreliminaryImpulse>();
        var sortedImpulses = nonNullImpulses
            .Where(i => i.IsLong != isLong && i.WasIntersection)
            .OrderByDescending(impulse => impulse.AverageZPercentage)
            .Take(topCount)
            .ToList();
        return sortedImpulses;
    }
}