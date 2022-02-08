using Discord.Commands;
using System.Reflection;

namespace SocuciusErgallaBot.Managers
{
    internal static class CommandManager
    {
        private static CommandService _commandService = ServiceManager.GetService<CommandService>();

        public static async Task LoadCommandsAsync()
        {
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(),ServiceManager.Provider);
            foreach (var command in _commandService.Commands)
            {
                Console.WriteLine($"Command {command.Name} loaded");
            }
        }

        public static async Task<string> GetCommandHelp()
        {
            var commands = _commandService.Commands.ToList();
            string help = $"Help\nUsage: {ConfigManager.Config.Prefix}[Commandname] [paramaters]\n\tEx: {ConfigManager.Config.Prefix}{commands.First().Name}\n\nSee below for a list of commands available:\n[Command Name](Alias):[Description]\n";
            foreach (var command in commands)
            {
                help += $"{ConfigManager.Config.Prefix}{command.Name}\t({GetAliases(command.Aliases)}): \t{command.Summary}\n";
            }
            return help;
        }

        private static string GetAliases(IReadOnlyList<string> aliases)
        {
            string aliasString = string.Empty;
            foreach (var alias in aliases)
            {
                aliasString+= alias;
                if(alias != aliases.Last())
                {
                    aliasString += ", ";
                }
            }
            return aliasString;
        }
    }
}
