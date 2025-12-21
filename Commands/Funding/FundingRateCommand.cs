using FuturesSignalsBot.Core;
using FuturesSignalsBot.Services.Analysis;
using FuturesSignalsBot.Services.Trading;
using Telegram.Bot.Types;

namespace FuturesSignalsBot.Commands.Funding;

public class FundingRateCommand : BaseCommand
{
    public override string Name => "/funding_rate";

    public override async Task ExecuteAsync(Message message, Core.User currentUser, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin)
        {
            await GlobalClients.TelegramBotService.SendMessageToChatAsync(
                message.Chat.Id,
                "Доступ к команде ограничен",
                cancellationToken: cancellationToken);
            return;
        }

        if (!AnalysisOrchestrator.AreNotificationsPrepared)
        {
            await GlobalClients.TelegramBotService.SendMessageToChatAsync(
                message.Chat.Id,
                "Выполняются расчеты фона рынка. Повторите запрос позже...",
                cancellationToken: cancellationToken);
            return;
        }

        await MarketFundingAnalyzer.SendFundingReportAsync(message.Chat.Id);
    }
}