using FuturesSignalsBot.Core;
using FuturesSignalsBot.Enums;
using FuturesSignalsBot.Services.Notifiers;
using FuturesSignalsBot.Services.Trading;
using Telegram.Bot.Types;

namespace FuturesSignalsBot.Commands.Funding;

public class ShortReverseCommand : BaseCommand
{
    public override string Name => "/short_reverse";

    public override async Task ExecuteAsync(Message message, Core.User currentUser, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin) return;

        if (!AnalysisOrchestrator.AreNotificationsPrepared)
        {
            await GlobalClients.TelegramBotService.SendMessageToChatAsync(
                message.Chat.Id,
                "Выполняются расчеты. Повторите запрос позже...",
                cancellationToken: cancellationToken);
            return;
        }

        await LiquidationNotifier.SendTopLiquidationData(LiquidationLevelTopType.ShortReverseNarrative, message.Chat.Id);
    }
}