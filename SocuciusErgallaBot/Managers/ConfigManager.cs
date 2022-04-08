using Newtonsoft.Json;

namespace SocuciusErgallaBot.Managers
{
    internal static class ConfigManager
    {
        private static string ConfigFolder = "Resources";
        private static string ConfigFile = "config.json";
        private static string ServerInfo = "tes3mpserverinfo.json";
        private static string ConfigPath = Path.Join(ConfigFolder, ConfigFile);
        private static string ServerPath = Path.Join(ConfigFolder, ServerInfo);
        public static BotConfig Config { get; private set; }
        public static TES3MPServer TES3MPServer { get; private set; }

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

            if (!File.Exists(ServerPath))
            {
                TES3MPServer = new TES3MPServer();
                var json = JsonConvert.SerializeObject(TES3MPServer, Formatting.Indented);
                File.WriteAllText(ServerPath, json);
            }
            else
            {
                var json = File.ReadAllText(ServerPath);
                TES3MPServer = JsonConvert.DeserializeObject<TES3MPServer>(json);
            }
        }
    }

    internal struct BotConfig
    {
        //bot token for authentication from Discord
        [JsonProperty("token")]
        public string Token { get; private set; }
        //name of database
        [JsonProperty("historydatabase")]
        public string HistoryDatabase { get; private set; }
    }

    internal struct TES3MPServer
    {
        [JsonProperty("serverinfo")]
        public string[] ServerInfo { get; private set; }
        public string[] InstallInstructions { get; private set; }
        public string[] FallbackArchives { get; private set; }
        public string InstallDirectory { get;private set; }
        public string[] Mods { get; private set; }
    }
}
