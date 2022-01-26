using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace SocuciusErgallaBot.Managers
{
    internal class EventManager
    {
        private static DiscordSocketClient _client = ServiceManager.GetService<DiscordSocketClient>();
        private static CommandService _commandService = ServiceManager.GetService<CommandService>();
        private static LavaNode _lavaNode = ServiceManager.Provider.GetRequiredService<LavaNode>();
        private static System.Timers.Timer _statusChangeTimer;
        private static System.Timers.Timer _stopMusicTimer;

        internal static Task LoadCommands()
        {
            _client.Log += message =>
            {
                Console.WriteLine($"({DateTime.Now})\t{message.Source}\t{message.Message}");
                return Task.CompletedTask;
            };

            _commandService.Log += message =>
            {
                Console.WriteLine($"({DateTime.Now})\t{message.Source}\t{message.Message}");
                return Task.CompletedTask;
            };

            _client.Ready += OnReady;
            _client.MessageReceived += OnMessageReceived;
            _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;
            return Task.CompletedTask;
        }

        private static async Task _client_UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            //bot leave timer is running but joined a new voice channel
            if(arg1.IsBot && _stopMusicTimer?.Enabled == true && arg3.VoiceChannel != null)
            {
                await StopLeaveChannelTimer();
            }

            if (arg1.IsBot)
                return;
            
            var mutualGuild = arg1.MutualGuilds.First();
            IVoiceChannel playerChannel = null;
            try
            {
                playerChannel = AudioManager.GetCurrentChannel(mutualGuild).Result;
            }catch (KeyNotFoundException)
            {
                //no player found
                return;
            }
            if(_stopMusicTimer?.Enabled == true && playerChannel == arg3.VoiceChannel)
                await StopLeaveChannelTimer();

            var channelLeft = arg2.VoiceChannel;

            //user left same channel as player and the user list only consists of the bot
            if (channelLeft?.Id == playerChannel.Id && channelLeft.Users.Count == 1)
            {
                await StartLeaveChannelTimer(playerChannel, mutualGuild);
            }
        }

        private static async Task OnMessageReceived(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(_client, message);
            if (message.Author.IsBot || message.Channel is not IDMChannel) return;

            var argPos = 0;

            if (!(message.HasStringPrefix(ConfigManager.Config.Prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;

            var result = await _commandService.ExecuteAsync(context, argPos, ServiceManager.Provider);

            if (!result.IsSuccess)
            {
                if (result.Error == CommandError.UnknownCommand) return;
            }
        }

        private static async Task OnReady()
        {
            try
            {
                await _lavaNode.ConnectAsync();

            }
            catch (Exception)
            {
                throw;
            }

            Console.WriteLine($"({DateTime.Now})\t (READY)\tBot is ready.");
            await _client.SetStatusAsync(Discord.UserStatus.Online);
            await SetNowPlayingTimer();
        }

        public static async Task SetNowPlayingTimer()
        {
            var trackInfo = Utility.SoundtrackInfo.GetRandomTrack();
            if (_statusChangeTimer is null)
            {
                _statusChangeTimer = new System.Timers.Timer(trackInfo.Duration);
                _statusChangeTimer.Elapsed += StatusChangeTimer_Elapsed;
                _statusChangeTimer.AutoReset = true;
            }
            else
            {
                _statusChangeTimer.Stop();
                _statusChangeTimer.Interval = trackInfo.Duration;
            }

            _statusChangeTimer.Start();
            await _client.SetGameAsync($"{trackInfo.Title}", null, Discord.ActivityType.Listening);
        }

        public static void StopNowPlayingTimer()
        {
            _statusChangeTimer.Stop();
        }

        private static Task StartLeaveChannelTimer(IVoiceChannel voiceChannel, IGuild guild)
        {
            if (_stopMusicTimer?.Enabled == true)
                return Task.CompletedTask;

            int leaveBuffer = 30000;
            if(_stopMusicTimer is null)
            {
                _stopMusicTimer = new(leaveBuffer);
                _stopMusicTimer.AutoReset = false;
                _stopMusicTimer.Elapsed += async (sender, e) =>
                {
                    Console.WriteLine($"Leave timer elapsed.");
                    await AudioManager.LeaveAsync(voiceChannel, guild);
                };
            }
            _stopMusicTimer.Start();
            Console.WriteLine($"Leave timer started");
            return Task.CompletedTask;
        }

        private static Task StopLeaveChannelTimer()
        {
            if (_stopMusicTimer.Enabled)
            {
                Console.WriteLine("Leave timer stopped");
                _stopMusicTimer.Stop();
            }
            return Task.CompletedTask;
        }

        private static async void StatusChangeTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            var trackInfo = Utility.SoundtrackInfo.GetRandomTrack();
            _statusChangeTimer.Stop();
            _statusChangeTimer.Interval = trackInfo.Duration;
            await _client.SetGameAsync($"{trackInfo.Title}", null, Discord.ActivityType.Listening);
            _statusChangeTimer.Start();
        }
    }
}
