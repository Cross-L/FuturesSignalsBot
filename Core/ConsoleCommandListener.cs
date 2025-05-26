namespace FuturesSignalsBot.Core
{
    public static class ConsoleCommandListener
    {
        public static async Task ListenForCommands()
        {
            var commandTask = MonitorTextCommands();
            await commandTask;
        }

        private static async Task MonitorTextCommands()
        {
            var inputTask = Task.Run(Console.ReadLine);

            while (true)
            {
                var completedTask = await Task.WhenAny(inputTask);

                if (completedTask == inputTask)
                {
                    inputTask = Task.Run(Console.ReadLine);
                }
            }
        }
    }
}
