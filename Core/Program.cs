using FuturesSignalsBot.Core.Config;

namespace FuturesSignalsBot.Core
{
    internal static class Program
    {
        private static async Task Main()
        {
            Console.CancelKeyPress += (_, e) =>
            {
                Console.WriteLine("Завершение работы...");
                e.Cancel = true;

                OnProcessExit().GetAwaiter().GetResult();
                Environment.Exit(0);
            };

            const string pathToConfigFile = "appsettings.json";
            var appConfig = await AppConfigLoader.LoadConfigAsync(pathToConfigFile);

            if (appConfig is null)
            {
                Console.WriteLine("Ошибка! Файл конфигурации не найден!");
                return;
            }

            try
            {
                await AnalysisCore.Init(appConfig);
                var tradingTask = Task.Run(AnalysisCore.ExecuteTradeAsync);
                var commandListenerTask = Task.Run(ConsoleCommandListener.ListenForCommands);
                await Task.WhenAll(tradingTask, commandListenerTask);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Ошибка: {ex.Message}\nМесто возникновения: {ex.StackTrace}";
                await GlobalClients.TelegramBotService.SendMessageToAdminsAsync(errorMessage);
                throw;
            }
        }

        private static async Task OnProcessExit()
        {
            await GlobalClients.TelegramBotService.Stop();
            await AnalysisCore.SaveUsersDataAsync();
            Console.WriteLine("Программа успешно завершила работу");
        }
    }
}