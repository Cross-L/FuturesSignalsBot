using System.Collections.Generic;

namespace FuturesSignalsBot.Models.Config;

public class AppConfig
{
    public Dictionary<string, UserConfig> UserConfigs { get; set; }
    public TelegramBotConfig TelegramBotConfig { get; set; }
}