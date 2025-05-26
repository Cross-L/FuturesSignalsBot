using FuturesSignalsBot.Enums;

namespace FuturesSignalsBot.NameFactories;

public static class PositionTypeNameFactory
{
    private static readonly Dictionary<PositionType, string> PositionTypeNames = new()
    {
        { PositionType.NotSet, "Не установлена" },
        { PositionType.Standard, "Обычная" },
        { PositionType.ReEntry, "Сделка перезахода" },
        { PositionType.HighVolatility, "Высоковолатильная" },
        { PositionType.BigShort, "BigShort" },
        { PositionType.BigLong, "BigLong" },
        { PositionType.Level33Sex, "Шестерка" },
    };

    public static string GetPositionTypeName(PositionType type)
    {
        return PositionTypeNames.GetValueOrDefault(type, "Неизвестно");
    }
}