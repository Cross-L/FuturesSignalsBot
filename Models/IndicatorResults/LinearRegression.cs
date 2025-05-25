namespace FuturesSignalsBot.Models.IndicatorResults;

public class LinearRegression(decimal upperBand, decimal lowerBand)
{
    public decimal UpperBand { get; set; } = upperBand;

    public decimal LowerBand { get; set; } = lowerBand;
}