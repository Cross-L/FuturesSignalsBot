using FuturesSignalsBot.Core;
using FuturesSignalsBot.Services.Analysis;
using FuturesSignalsBot.Services.Trading;
using Telegram.Bot.Types;

namespace FuturesSignalsBot.Commands.SignalCommands.Tmo;
using User = Core.User;

public class TmoIndexCommand: BaseCommand
{
    public override string Name => "/tmo_index";

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
        
        if (!AnalysisOrchestrator.AreNotificationsPrepared)
        {
             await GlobalClients.TelegramBotService.SendMessageToChatAsync(message.Chat.Id,
                "Выполняются расчеты. Повторите запрос позже...",
                cancellationToken: cancellationToken);
            return;
        }
        
        await TmoIndexAnalyzer.SendTmoIndexReportAsync(message.Chat.Id);
    }
}