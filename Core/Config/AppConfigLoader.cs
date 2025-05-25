using FuturesSignalsBot.Models.Config;
using Newtonsoft.Json;

namespace FuturesSignalsBot.Core.Config;

public static class AppConfigLoader
{
    public static async Task<AppConfig?> LoadConfigAsync(string path)
    {
        using var reader = new StreamReader(path);
        var json = await reader.ReadToEndAsync();
        return JsonConvert.DeserializeObject<AppConfig>(json);
    }
}