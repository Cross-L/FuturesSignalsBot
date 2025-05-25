using System;
using System.Threading;
using System.Threading.Tasks;
using FuturesSignalsBot.Commands;
using FuturesSignalsBot.Core;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FuturesSignalsBot.Services.Bot;

public class BotHandlers: IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        if (update is { Type: UpdateType.Message, Message.Text: not null })
        {
            Console.WriteLine($"Пользователь: {update.Message.From?.Username}. Текст: {update.Message.Text} " +
                              $"Время: {DateTimeOffset.UtcNow:dd.MM.yyyy HH:mm:ss zzz}");
            var message = update.Message;
            Trader.Users.TryGetValue(message.From!.Id, out var currentUser);
            
            var commandText = message.Text.ToLower().Split('@')[0].Trim();
            var command = CommandFactory.GetCommand(commandText);
            
            if (command != null)
            {
                if (currentUser is not null || message.Text.ToLower().Equals("/start"))
                {
                    await command.ExecuteAsync(message, currentUser!, cancellationToken);
                }
            }
        }
    }
    
    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        if (exception.Message.Contains("Request timed out"))
        {
            return Task.CompletedTask;
        }
        
        Console.WriteLine($"Ошибка (Telegram, {source}): {exception.Message}\nСтрока: {exception.StackTrace} Время: {DateTime.UtcNow}");
        return Task.CompletedTask;
    }
}