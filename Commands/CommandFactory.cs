using FuturesSignalsBot.Commands.SignalCommands;
using FuturesSignalsBot.Commands.SignalCommands.Absorption;
using FuturesSignalsBot.Commands.SignalCommands.Correlation;
using FuturesSignalsBot.Commands.SignalCommands.Price;
using FuturesSignalsBot.Commands.SignalCommands.Tmo;

namespace FuturesSignalsBot.Commands;

public static class CommandFactory
{
    private static readonly List<BaseCommand> Commands =
    [
        new StartCommand(),
        new StatusCommand(),
        new ReceivedSignalsCommand(),
        new SeriesCorrelationCommand(),
        new AltCoinDelayCommand(),
        new TmoIndexCommand(),
        new LongTmoInefficiencyCommand(),
        new ShortTmoInefficiencyCommand(),
        new AbovePocCommand(),
        new BelowPocCommand(),
        new LowTopCommand(),
        new HighTopCommand(),
        new MaxExtremeCorrelationCommand(),
        new MinExtremeCorrelationCommand(),
        new AntiExtremeCorrelationCommand(),
    ];
    
    public static BaseCommand? GetCommand(string commandName) 
        => Commands.FirstOrDefault(c => c.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
    
}