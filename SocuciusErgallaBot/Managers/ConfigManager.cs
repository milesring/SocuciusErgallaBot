using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocuciusErgallaBot.Managers
{
    internal static class ConfigManager
    {
        private static string ConfigFolder = "Resources";
        private static string ConfigFile = "config.json";
        private static string ConfigPath = Path.Join(ConfigFolder, ConfigFile);
        public static BotConfig Config { get; private set; }

        static ConfigManager()
        {
            if (!Directory.Exists(ConfigFolder))
            {
                Directory.CreateDirectory(ConfigFolder);
            }

            if (!File.Exists(ConfigPath))
            {
                Config = new BotConfig();
                var json = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            else
            {
                var json = File.ReadAllText(ConfigPath);
                Config = JsonConvert.DeserializeObject<BotConfig>(json);
            }
        }
    }

    internal struct BotConfig
    {
        //bot token for authentication from Discord
        [JsonProperty("token")]
        public string Token { get; private set; }
        //prefix used in calling commands. !m, !bot, bot, etc.
        [JsonProperty("prefix")]
        public string Prefix { get; private set; }
    }
}
