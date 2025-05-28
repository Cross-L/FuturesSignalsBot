using FuturesSignalsBot.Enums;
using FuturesSignalsBot.Services.User;

namespace FuturesSignalsBot.Core;

public class User(long userId, string name, bool isAdmin)
{
    public string Name { get; } = name;
    public bool IsAdmin { get; } = isAdmin;
    public UserDataService DataService { get; } = new(userId);
}