using System;
using FuturesSignalsBot.Enums;

namespace FuturesSignalsBot.NameFactories;

public static class PlacementNameFactory
{
    public static string GetPlacementName(AccountPlacement placement)
    {
        return placement switch
        {
            AccountPlacement.Main => "Основной аккаунт",
            AccountPlacement.Sub => "Субаккаунт",
            AccountPlacement.Reserve => "Резервный аккаунт",
            _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, "Недопустимое значение размещения")
        };
    }
}