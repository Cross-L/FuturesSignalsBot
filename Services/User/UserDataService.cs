using System.Text.Json;
using FuturesSignalsBot.Models;

namespace FuturesSignalsBot.Services.User;

public class UserDataService
{
    private readonly string _baseFolder;
    
    public UserData Data { get; set; }
    
    public UserDataService(long userId)
    {
        var baseDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.FullName;
        if (baseDirectory == null)
        {
            throw new InvalidOperationException("Cannot determine parent directory!");
        }

        _baseFolder = Path.Combine(baseDirectory, "UserData", userId.ToString());
    }
    
    /// <summary>
    /// Метод для сохранения контейнера в файл.
    /// </summary>
    public async Task SaveUserDataAsync()
    {
        var directoryPath = Path.Combine(_baseFolder);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var filePath = Path.Combine(directoryPath, "container.json");
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(Data, jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Метод для загрузки контейнера из файла.
    /// </summary>
    public async Task LoadUserDataAsync()
    {
        var directoryPath = Path.Combine(_baseFolder);
        var filePath = Path.Combine(directoryPath, "container.json");

        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath);
            Data = JsonSerializer.Deserialize<UserData>(json) ?? new UserData();
        }
        else
        {
            Data = new UserData();
        }
    }

}