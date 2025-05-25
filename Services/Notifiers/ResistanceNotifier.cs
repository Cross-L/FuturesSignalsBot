using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FuturesSignalsBot.Core;
using FuturesSignalsBot.Enums;
using FuturesSignalsBot.Models.Resistance;
using FuturesSignalsBot.NameFactories;
using FuturesSignalsBot.Services.Analysis;

namespace FuturesSignalsBot.Services.Notifiers;

public static class ResistanceNotifier
{
    public static async Task SendTopCorrelationData(CorrelationType correlationType, long chatId = 0)
    {
        var extremeTopList = correlationType switch
        {
            CorrelationType.MaxExtremeCorrelation => CorrelationTrendAnalyzer.MaxExtremeTop,
            CorrelationType.MinExtremeCorrelation => CorrelationTrendAnalyzer.MinExtremeTop,
            CorrelationType.AntiExtremeCorrelation => CorrelationTrendAnalyzer.AntiExtremeTop,
            CorrelationType.MaxSeriesCorrelation => CorrelationTrendAnalyzer.MaxSeriesTop,
            _ => throw new ArgumentOutOfRangeException(nameof(correlationType), correlationType, null)
        };

        var message = await CreateCorrelationMessage(extremeTopList, correlationType);

        if (chatId is 0)
        {
            await GlobalClients.TelegramBotService.SendMessageToGroup(message);
        }
        else
        {
            await GlobalClients.TelegramBotService.SendMessageToChatAsync(chatId, message);
        }
    }

    private static async Task<string> CreateCorrelationMessage(List<BasicResistanceInfo> extremeTopList,
        CorrelationType correlationType)
    {
        if (extremeTopList.Count == 0)
        {
            return $"<b>{CorrelationNameFactory.GetCorrelationNameByType(correlationType)}:</b>\n- Валют нет.";
        }

        var firstItem = extremeTopList[0];
        var referenceStartOpenTime = firstItem.SeriesStart.OpenTime;
        var referenceEndOpenTime = firstItem.SeriesEnd.OpenTime;

        foreach (var item in extremeTopList)
        {
            if (item.SeriesStart.OpenTime != referenceStartOpenTime ||
                item.SeriesEnd.OpenTime != referenceEndOpenTime)
            {
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(
                    $"⚠️OpenTime не совпадают для валюты: {item.AltCoinName}");
                break;
            }
        }

        var bitcoinSeriesSide = firstItem.IsBtcSeriesIncreased ? "📈 Рост" : "📉 Падение";
        var bitcoinExtremeSide = firstItem.IsBtcExtremeIncreased ? "📈 Рост" : "📉 Падение";
        var altExtremeSide = firstItem.IsAltCoinExtremeIncreased ? "📈 Рост" : "📉 Падение";
        var altSeriesSide = firstItem.IsAltCoinSeriesIncreased ? "📈 Рост" : "📉 Падение";

        const string dateTimeFormat = "dd/MM/yyyy HH:mm:ss 'UTC +02:00'";

        var messageBuilder = new StringBuilder();

        messageBuilder.AppendLine($"<b>{CorrelationNameFactory.GetCorrelationNameByType(correlationType)}:</b>\n");

        messageBuilder.AppendLine(
            $"<b>📅 Начало серии:</b> {firstItem.SeriesStart.OpenTime.AddHours(2).ToString(dateTimeFormat)}");
        messageBuilder.AppendLine(
            $"<b>📅 Конец серии:</b> {firstItem.SeriesEnd.CloseTime.AddHours(2).ToString(dateTimeFormat)}");

        messageBuilder.AppendLine(
            $"<b>🔽 Min BTC:</b> {firstItem.BtcMinItem.OpenTime.AddHours(2).ToString(dateTimeFormat)}");
        messageBuilder.AppendLine(
            $"<b>🔼 Max BTC:</b> {firstItem.BtcMaxItem.OpenTime.AddHours(2).ToString(dateTimeFormat)}");

        messageBuilder.AppendLine(
            $"<b>{bitcoinSeriesSide} BTC</b> на <b>{firstItem.SeriesBtcChange:F3}%</b> в рамках общей серии");
        messageBuilder.AppendLine(
            $"<b>{bitcoinExtremeSide} BTC</b> на <b>{firstItem.ExtremeBtcChange:F3}%</b> в рамках мин/макс");

        messageBuilder.AppendLine("\n<b>🔹 Топ Альткоинов:</b>");

        for (var i = 0; i < extremeTopList.Count; i++)
        {
            var item = extremeTopList[i];
            switch (correlationType)
            {
                case CorrelationType.AntiExtremeCorrelation:
                    messageBuilder.AppendLine(
                        $"<b>{i + 1}. {altExtremeSide} {item.AltCoinName}.P</b> на <b>{item.ExtremeMultiplier:F3}</b> или <b>{item.ExtremeAltCoinChange:F3}%</b>\n" +
                        $"<i>{altSeriesSide} {item.AltCoinName}</i> в рамках серии BTC на <b>{item.SeriesMultiplier:F3}</b> или <b>{item.SeriesAltCoinChange:F3}%</b>");
                    break;
                case CorrelationType.MaxSeriesCorrelation:
                    messageBuilder.AppendLine(
                        $"<b>{i + 1}. {altExtremeSide} {item.AltCoinName}.P</b> на <b>{item.SeriesMultiplier:F3}</b> или <b>{item.SeriesAltCoinChange:F3}%</b> в рамках серии");
                    break;
                default:
                    messageBuilder.AppendLine(
                        $"<b>{i + 1}. {altExtremeSide} {item.AltCoinName}.P</b> на <b>{item.ExtremeMultiplier:F3}</b> или <b>{item.ExtremeAltCoinChange:F3}%</b>");
                    break;
            }
        }

        return messageBuilder.ToString();
    }

    public static async Task SendMaxDelays(long chatId)
    {
        var messageBuilder = new StringBuilder();

        messageBuilder.AppendLine("<b>Общее количество импульсов BTC:</b> " +
                                  $"<b>{BitcoinResistanceService.ResistanceSeries.Count}</b>");

        messageBuilder.AppendLine("<b>🔝 Топ Задержек:</b>");

        for (var i = 0; i < CorrelationTrendAnalyzer.TopDelays.Count; i++)
        {
            var item = CorrelationTrendAnalyzer.TopDelays[i];
            messageBuilder.AppendLine(
                $"<b>{i + 1}. {item.name}.P</b> задержка - <b>{item.delay:F3}</b>. Кол-во импульсов: <b>{item.targetImpulses}</b>");
        }

        await GlobalClients.TelegramBotService.SendMessageToChatAsync(chatId,messageBuilder.ToString());
    }
}