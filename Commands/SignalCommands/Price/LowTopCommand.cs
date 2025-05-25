using System.Threading;
using System.Threading.Tasks;
using FuturesSignalsBot.Core;
using FuturesSignalsBot.Enums;
using FuturesSignalsBot.Services.Notifiers;
using FuturesSignalsBot.Services.Trading;
using Telegram.Bot.Types;

namespace FuturesSignalsBot.Commands.SignalCommands.Price;
using User = Core.User;

public class LowTopCommand: BaseCommand
{
    public override string Name => "/low_top";

    public override async Task ExecuteAsync(Message message, User currentUser,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin)
        {
             await GlobalClients.TelegramBotService.SendMessageToChatAsync(message.Chat.Id,
                 "Доступ к команде ограничен",
                cancellationToken: cancellationToken);
            return;
        }
        
        if (!TradingOrchestrator.AreNotificationsPrepared)
        {
             await GlobalClients.TelegramBotService.SendMessageToChatAsync(message.Chat.Id,
                "Выполняются расчеты. Повторите запрос позже...",
                cancellationToken: cancellationToken);
            return;
        }
        
        await LiquidationNotifier.SendTopLiquidationData(LiquidationLevelTopType.Low, message.Chat.Id);
    }
}