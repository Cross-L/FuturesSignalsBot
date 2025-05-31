using FuturesSignalsBot.Core;
using FuturesSignalsBot.Models.Config;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FuturesSignalsBot.Services.Bot;

public class TelegramBotService
{
    private readonly TelegramBotClient _botClient;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly BotHandlers _botHandlers;
    private readonly long _groupId;
    private readonly long _secondGroupId;
    private readonly List<long> _userChatIds;

    public TelegramBotService(AppConfig appConfig)
    {
        _groupId = appConfig.TelegramBotConfig.GroupId;
        _secondGroupId = appConfig.TelegramBotConfig.SecondGroupId;
        _botHandlers = new BotHandlers();
        _userChatIds = appConfig.UserConfigs.Keys.Select(long.Parse).ToList();
        
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
        
        _botClient = new TelegramBotClient(appConfig.TelegramBotConfig.BotToken, httpClient);
    }

    private async Task SetCommands()
    {
        var commands = new List<BotCommand>
        {
            new() { Command = "received_signals", Description = "Полученные сигналы" },
            new() { Command = "switch_currency", Description = "Включить/Отключить обработку валюты" },
            new() { Command = "status", Description = "Статус бота" }
        };

        await _botClient.SetMyCommands(commands);
    }

    public async Task Start()
    {
        await SendMessageToAllUsersAsync("Бот запущен!");
        await SetCommands();
        var receiverOptions = new ReceiverOptions();
        _botClient.StartReceiving(_botHandlers, receiverOptions, _cancellationTokenSource.Token);
    }

    public async Task Stop()
    {
        await SendMessageToAllUsersAsync("Бот остановлен!");
        await _cancellationTokenSource.CancelAsync();
    }

    private async Task SendMessageToAllUsersAsync(string message)
    {
        foreach (var chatId in _userChatIds)
        {
            try
            {
                await _botClient.SendMessage(chatId, message);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка отправки сообщения в чат {chatId}: {e.Message}");
            }
        }
    }
    
    public async Task SendMessageToChatAsync(long chatId, string text, ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        await SendWithRetries(async () =>
        {
            await _botClient.SendMessage(chatId, text, ParseMode.Html, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
        });
    }
    
    public async Task SendMessageToAdminsAsync(string message)
    {
        foreach (var (userId, user) in AnalysisCore.Users)
        {
            if (user.IsAdmin)
            {
                try
                {
                    await _botClient.SendMessage(userId, message);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Ошибка отправки сообщения админу {userId}: {e.Message}");
                }
            }
        }
    }
    
    public async Task SendMessageToGroup(string message)
    {
        await SendWithRetries(async () =>
        {
            await _botClient.SendMessage(_groupId, message, parseMode: ParseMode.Html);
        });
    }
    
    public async Task SendMessageToSecondGroup(string message)
    {
        await SendWithRetries(async () =>
        {
            await _botClient.SendMessage(_secondGroupId, message, parseMode: ParseMode.Html);
        });
    }

    public async Task<string> GetUsernameByIdAsync(long id)
    {
        try
        {
            var chat = await _botClient.GetChat(id);
            return chat.Username ?? "N/A";
        }
        catch
        {
            return "N/A";
        }
    }
    
    public async Task SendFileToChatAsync(long chatId, string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Файл по пути {filePath} не найден.");
            return;
        }
        
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
        {
            await _botClient.SendMessage(chatId, "Файл мониторинга пуст");
            return;
        }

        var fileName = Path.GetFileName(filePath);

        try
        {
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var inputFile = new InputFileStream(stream, fileName);
            await _botClient.SendDocument(chatId, inputFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при отправке файла в чат {chatId}: {ex.Message}");
        }
    }
    
    private static async Task SendWithRetries(Func<Task> sendAction)
    {
        const int maxRetries = 3;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await sendAction();
                return;
            }
            catch (TaskCanceledException)
            {
                if (attempt == maxRetries)
                {
                    Console.WriteLine("Достигнуто максимальное количество попыток отправки сообщений. Завершение работы метода.");
                    throw;
                }
                await Task.Delay(10000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка во время отправки сообщения: {ex.Message}");
                throw;
            }
        }
    }
}