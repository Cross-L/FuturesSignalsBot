using FuturesSignalsBot.Core;
using FuturesSignalsBot.Enums;
using Telegram.Bot.Types;

namespace FuturesSignalsBot.Commands;
using User = Core.User;

public class SwitchCurrencyCommand: BaseCommand
{
    public override string Name => "/switch_currency";

    public override async Task ExecuteAsync(Message message, User currentUser, CancellationToken cancellationToken)
    {
        await GlobalClients.TelegramBotService.SendMessageToChatAsync(message.Chat.Id, 
            $"❌ Отключены следующие символы: {string.Join(", ", currentUser.DataService.Data.DisabledCurrencies)}" +
            $" \nВведите символ, который хотите включить или отключить:", 
            cancellationToken: cancellationToken);
        currentUser.DataService.Data.State = UserState.CurrencySwitching;
    }
}