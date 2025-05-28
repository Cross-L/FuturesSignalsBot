using FuturesSignalsBot.Enums;

namespace FuturesSignalsBot.Models;

public class UserData
{
    public List<string> DisabledCurrencies { get; set; } = [];
    
    public UserState State { get; set; }
}