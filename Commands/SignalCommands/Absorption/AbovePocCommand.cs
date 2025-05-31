using FuturesSignalsBot.Core;
using FuturesSignalsBot.Enums;
using FuturesSignalsBot.Services.Notifiers;
using FuturesSignalsBot.Services.Trading;
using Telegram.Bot.Types;

namespace FuturesSignalsBot.Commands.SignalCommands.Absorption;
using User = Core.User;

public class AbovePocCommand: BaseCommand
{
    public override string Name => "/above_poc";

    public override async Task ExecuteAsync(Message message, User currentUser, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin)
        {
            await GlobalClients.TelegramBotService.SendMessageToChatAsync(message.Chat.Id,
                "Доступ к команде ограничен", cancellationToken: cancellationToken);
            return;
        }
        
        if (!AnalysisOrchestrator.AreNotificationsPrepared)
        {
            await GlobalClients.TelegramBotService.SendMessageToChatAsync(message.Chat.Id,
                "Выполняются расчеты. Повторите запрос позже...", cancellationToken: cancellationToken);
            return;
        }
        
        await LiquidationNotifier.SendTopLiquidationData(LiquidationLevelTopType.HigherPoc, message.Chat.Id);
    }
}