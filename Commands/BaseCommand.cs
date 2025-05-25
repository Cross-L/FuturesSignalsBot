using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using User = FuturesSignalsBot.Core.User;

namespace FuturesSignalsBot.Commands;

public abstract class BaseCommand
{
    public abstract string Name { get; }

    public abstract Task ExecuteAsync(Message message, User currentUser,
        CancellationToken cancellationToken);
}