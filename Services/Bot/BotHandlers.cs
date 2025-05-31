using FuturesSignalsBot.Commands;
using FuturesSignalsBot.Core;
using FuturesSignalsBot.Enums;
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
            AnalysisCore.Users.TryGetValue(message.From!.Id, out var currentUser);
            
            var commandText = message.Text.ToLower().Split('@')[0].Trim();
            var command = CommandFactory.GetCommand(commandText);
            
            if (command != null)
            {
                if (currentUser is not null || message.Text.ToLower().Equals("/start"))
                {
                    await command.ExecuteAsync(message, currentUser!, cancellationToken);
                }
            }
            else
            {
                if (currentUser is not null && currentUser.DataService.Data.State == UserState.CurrencySwitching)
                {
                    var currencyName = message.Text!.Trim().ToUpper();
                    if (!currencyName.Contains("USDT"))
                    {
                        currencyName += "USDT";
                    }

                    var targetCurrency = GlobalClients.CryptocurrenciesStorage.AllCryptocurrencies.FirstOrDefault
                        (x => x.Name == currencyName);

                    if (targetCurrency is not null)
                    {
                        if (currentUser.DataService.Data.DisabledCurrencies.Remove(currencyName))
                        {
                            await GlobalClients.TelegramBotService.SendMessageToChatAsync(message.Chat.Id,
                                $"✅ Обработка сигналов для {currencyName} включена",
                                cancellationToken: cancellationToken);
                        }
                        else
                        {
                            currentUser.DataService.Data.DisabledCurrencies.Add(currencyName);
                            await GlobalClients.TelegramBotService.SendMessageToChatAsync(message.Chat.Id,
                                $"❌ Обработка сигналов для {currencyName} остановлена", cancellationToken: cancellationToken);
                        }
                    }
                    else
                    {
                        await GlobalClients.TelegramBotService.SendMessageToChatAsync(message.Chat.Id, $"Символа " +
                            $"{currencyName} нету в списка обработки сигналов", cancellationToken: cancellationToken);
                    }
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