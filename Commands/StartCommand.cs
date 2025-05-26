using FuturesSignalsBot.Core;
using Telegram.Bot.Types;
using User = FuturesSignalsBot.Core.User;

namespace FuturesSignalsBot.Commands;

public class StartCommand : BaseCommand
{
    public override string Name => "/start";

    public override async Task ExecuteAsync(Message message, User? currentUser, CancellationToken cancellationToken)
    {
        if (currentUser is null)
        {
             await GlobalClients.TelegramBotService.SendMessageToChatAsync(message.Chat.Id,
                "Для начала торговли необходимо отправить Binance ключи secretKey и apiKey для рассмотрения администратором " +
                "в формате:\nSecretKey: xxxxxxxx, ApiKey: xxxxxxxx", cancellationToken: cancellationToken);
            var username = await GlobalClients.TelegramBotService.GetUsernameByIdAsync(message.Chat.Id);
            await GlobalClients.TelegramBotService.SendMessageToAdminsAsync("К боту хочет присоединиться новый пользователь:\n" +
                                                                            $"Username: {username}, ID: {message.Chat.Id}");
        }
    }
}