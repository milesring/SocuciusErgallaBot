using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SocuciusErgallaBot.Managers;
using Victoria;

namespace SocuciusErgallaBot
{
    internal class Bot
    {
        private DiscordSocketClient _client;
        private CommandService _commandService;

        public Bot()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = Discord.LogSeverity.Debug
            });

            _commandService = new CommandService(new CommandServiceConfig()
            {
                LogLevel= Discord.LogSeverity.Debug,
                CaseSensitiveCommands = true,
                IgnoreExtraArgs = true,
                DefaultRunMode = RunMode.Async
            });

            var collection = new ServiceCollection();
            collection.AddSingleton(_client);
            collection.AddSingleton(_commandService);
            collection.AddLavaNode(x =>
            {
                x.SelfDeaf = false;
            });

            ServiceManager.SetProvider(collection);
        }

        public async Task MainAsync()
        {
            if (string.IsNullOrWhiteSpace(ConfigManager.Config.Token))
            {
                return;
            }
            await EventManager.LoadCommands();

            await _client.LoginAsync(Discord.TokenType.Bot, ConfigManager.Config.Token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }
    }
}
