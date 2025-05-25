using System.Threading;
using System.Threading.Tasks;
using FuturesSignalsBot.Core;
using Telegram.Bot.Types;
using User = FuturesSignalsBot.Core.User;

namespace FuturesSignalsBot.Commands;

public class StatusCommand : BaseCommand
{
    public override string Name => "/status";

    public override async Task ExecuteAsync(Message message, User currentUser, CancellationToken cancellationToken)
    {
        var runningServicesCount = await Trader.GetHealthStatus(message.Chat.Id);
        
         await GlobalClients.TelegramBotService.SendMessageToChatAsync(message.Chat.Id,
             $"Работающих сервисов: {runningServicesCount}", cancellationToken: cancellationToken);
    }
}