using System.Collections.Generic;
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
        { "+50 Poc", 8 }, { "+33.3 Poc", 7 }, { "+20 Poc", 6 }, { "+10 Poc", 5 }, 
        { "+6.6 Poc", 4 }, { "+5 Poc", 3 }, { "+3.3 Poc", 2 }, { "+2 Poc", 1 },
        
        { "-2 Poc", 1 }, { "-3.3 Poc", 2 }, { "-5 Poc", 3 }, { "-6.6 Poc", 4 },
        { "-10 Poc", 5 }, { "-20 Poc", 6 }, { "-33.3 Poc", 7 }, { "-50 Poc", 8 }
    };

}