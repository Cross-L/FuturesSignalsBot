using FuturesSignalsBot.Enums;
using FuturesSignalsBot.Services.Notifiers;
using FuturesSignalsBot.Services.Trading;
using Telegram.Bot.Types;

namespace FuturesSignalsBot.Commands.Funding;

public class LongFundingRateCommand : BaseCommand
{
    public override string Name => "/long_frate";
    public override async Task ExecuteAsync(Message message, Core.User currentUser, CancellationToken ct)
    {
        if (!currentUser.IsAdmin) return;
        if (!AnalysisOrchestrator.AreNotificationsPrepared) return;

        await LiquidationNotifier.SendTopLiquidationData(LiquidationLevelTopType.LongFundingRate, message.Chat.Id);
    }
}