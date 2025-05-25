namespace FuturesSignalsBot.Models.IndicatorResults;

public class MarketAbsorptionValues
{
    public double AverageHigherPocPercentageChange { get; set; }
    public double AverageLowerPocPercentageChange { get; set; }

    public double HigherPocPercentage { get; set; }
    public double LowerPocPercentage { get; set; }

    public int HigherPocCount { get; set; }
    public int LowerPocCount { get; set; }
}