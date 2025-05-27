using System.Globalization;

namespace FuturesSignalsBot.Models;

public class LiquidationIntersectResult(decimal price, decimal numericalLevel)
{
    public decimal Price { get; } = price;
    public override string ToString() =>
        $"{(numericalLevel > 0 ? "+" : "")}{numericalLevel.ToString(CultureInfo.InvariantCulture)} Poc";

    public int LiquidationNumber => LiquidationMap[ToString()];

    private static readonly Dictionary<string, int> LiquidationMap = new()
    {
        { "+50 Poc", 5 }, { "+33.3 Poc", 4 }, { "+20 Poc", 3 }, { "+10 Poc", 2 }, { "+5 Poc", 1 },
        { "-5 Poc", 1 }, { "-10 Poc", 2 }, { "-20 Poc", 3 }, { "-33.3 Poc", 4 }, { "-50 Poc", 5 }
    };
    
    private static readonly Dictionary<int, int> LiquidationPercentageMap = new()
    {
        { 1, 15 }, { 2, 20 }, { 3, 20 }, { 4, 30 }, { 5, 40 }
    };
    public int LiquidationPercentage => LiquidationPercentageMap[LiquidationNumber];
}