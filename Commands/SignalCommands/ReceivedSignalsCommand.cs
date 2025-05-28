using FuturesSignalsBot.Core;
using Telegram.Bot.Types;
using User = FuturesSignalsBot.Core.User;

namespace FuturesSignalsBot.Commands.SignalCommands;

public class ReceivedSignalsCommand: BaseCommand
{
    public override string Name => "/received_signals";

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
        
        var messageText = GlobalClients.CryptocurrenciesStorage.AllCryptocurrencies.Count > 0
            ? "<b>📈 Long:</b>\n" +
              "<b>/long_tmo_inefficiency</b> - Неэффективность ТМО(-8.00)\n" +
              "<b>/low_top</b> - Оценка Лоя\n" +
              "<b>/above_poc</b> - Выше POC_0\n\n" +
              "<b>📉 Short:</b>\n" +
              "<b>/short_tmo_inefficiency</b> - Неэффективность ТМО(+8.00)\n" +
              "<b>/high_top</b> - Оценка Хая\n" +
              "<b>/below_poc</b> - Ниже POC_0\n\n" +
              "<b>💢 Z-Score:</b>\n" +
              "<b>/long_score</b> - 💢Long Z-score\n" +
              "<b>/short_score</b> - 💢Short Z-score\n" +
              "<b>/long_deliq</b> - 🪫Long Deliq\n" +
              "<b>/short_deliq</b> - 🔋Short Deliq\n" +
              "<b>/long_open_max</b> - 🍎Long OpenMax\n" +
              "<b>/short_open_min</b> - 🍏Short OpenMin\n\n" +
              "<b>🔄 Корреляция:</b>\n" +
              "<b>/series_correlation</b> - Наибольшая корреляция в рамках серии\n" +
              "<b>/max_correlation</b> - Наибольшая корреляция в рамках min/max BTC\n" +
              "<b>/min_correlation</b> - Наименьшая корреляция в рамках min/max BTC\n" +
              "<b>/anti_correlation</b> - Антикорреляция в рамках min/max BTC\n" +
              "<b>/altcoin_delay</b> - Наибольшая задержка альткоинов\n\n" +
              "<b>📐 TMO:</b>\n" +
              "<b>/tmo_index</b> - Индекс TMO\n\n"
            : "<b>❗ Сначала нужно добавить валюты для отслеживания сигналов</b>";
        
        await GlobalClients.TelegramBotService.SendMessageToChatAsync(message.Chat.Id, messageText, cancellationToken: cancellationToken);
    }
}