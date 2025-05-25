namespace FuturesSignalsBot.Core;

public class User(string name, bool isAdmin)
{
    public string Name { get; } = name;
    public bool IsAdmin { get; } = isAdmin;
}