namespace FuturesSignalsBot.Models;

public class LiquidationLevelContainer(decimal numericalLevel, decimal liquidationValue)
{
    public decimal NumericalLevel { get; set; } = numericalLevel;
    
    public decimal LiquidationValue { get; set; } = liquidationValue;
}