using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SocuciusErgallaBot.Managers;

namespace SocuciusErgallaBot.Modules
{
    [Name("Music")]
    public class MusicCommands : ModuleBase<SocketCommandContext>
    {
        private Random random = new Random();

        [Command("join", RunMode = RunMode.Async)]
        [Summary("Instructs the bot to join the voice channel you are in")]
        public async Task JoinCommand()
        {
            MusicResponse response = await JoinVoiceChannelAsync();
            await Context.Channel.SendMessageAsync(response.Message);
        }

        private async Task<MusicResponse> JoinVoiceChannelAsync()
        {
            SocketGuild guild = GetMutualGuild();
            var voiceChannel = guild.VoiceChannels.Where(x => x.Users.Any(y => y.Id == Context.Message.Author.Id)).FirstOrDefault();
            var response = await AudioManager.JoinAsync(voiceChannel, guild);
            return response;
        }

        private SocketGuild GetMutualGuild()
        {
            var guild = Context.Message.Author.MutualGuilds.First();
            return guild;
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Plays audio from youtube using a url or search terms")]
        public async Task PlayCommand([Remainder] string search)
        {
            var response = await PlayTrack(search);
            await Context.Channel.SendMessageAsync($"{response.Message}");
        }

        private async Task<MusicResponse> PlayTrack(string search)
        {
            var joinResponse = await JoinVoiceChannelAsync();
            if (joinResponse.Status == MusicResponseStatus.Error)
            {
                return joinResponse;
            }
            var guild = GetMutualGuild();
            var voiceChannel = guild.VoiceChannels.Where(x => x.Users.Any(y => y.Id == Context.Message.Author.Id)).First();
            var response = await AudioManager.PlayAsync(voiceChannel, guild, search, Context.Message.Author);
            EventManager.StopNowPlayingTimer();
            return response;
        }

        [Command("leave")]
        [Alias("stop")]
        [Summary("Stops playback and leaves the voice channel")]
        public async Task LeaveCommand()
        {
            var guild = GetMutualGuild();
            var userVoiceChannel = guild.VoiceChannels.Where(x => x.Users.Any(y => y.Id == Context.Message.Author.Id)).First();
            
            var response = await AudioManager.LeaveAsync(userVoiceChannel, guild);
            if(response.Status == MusicResponseStatus.Valid)
            {
                await EventManager.SetNowPlayingTimer();
            }
            await Context.Channel.SendMessageAsync($"{response.Message}");
        }

        [Command("volume")]
        [Alias("vol")]
        [Summary("Changes the bot's volume between 0 and 100. Default value is 10.")]
        public async Task VolumeCommand(int volume)
        {
            var guild = GetMutualGuild();
            var voiceChannel = guild.VoiceChannels.Where(x => x.Users.Any(y => y.Id == Context.Message.Author.Id)).FirstOrDefault();
            var response = await AudioManager.SetVolumeAsync(voiceChannel, guild, volume);
            await Context.Channel.SendMessageAsync($"{response.Message}");
        }

        [Command("next")]
        [Alias("skip")]
        [Summary("Changes track to next in queue")]
        public async Task NextCommand()
        {
            var guild = GetMutualGuild();
            var voiceChannel = guild.VoiceChannels.Where(x => x.Users.Any(y => y.Id == Context.Message.Author.Id)).FirstOrDefault();
            var response = await AudioManager.PlayNextAsync(voiceChannel, guild);
            if (response.Status == MusicResponseStatus.Error)
            {
                await Context.Channel.SendMessageAsync($"Error: {response.Message}");
            }
        }

        [Command("pause")]
        [Alias("resume", "res")]
        [Summary("Toggles the player between pause and playing state")]
        public async Task TogglePauseCommand() 
        {
            var guild = GetMutualGuild();
            var voiceChannel = guild.VoiceChannels.Where(x => x.Users.Any(y => y.Id == Context.Message.Author.Id)).FirstOrDefault();
            var response = await AudioManager.TogglePauseAsync(voiceChannel, GetMutualGuild());
            await Context.Channel.SendMessageAsync($"{response.Message}");
        }

        [Command("queue")]
        [Summary("Gets the queue of tracks from the current player")]
        public async Task QueueCommand()
            => await Context.Channel.SendMessageAsync(await Task.Run(() => AudioManager.GetQueue(GetMutualGuild())));

        [Command("nowplaying")]
        [Alias("np")]
        [Summary("Sends a message with the currently playing track")]
        public async Task NowPlayingCommand()
        {
            var nowPlaying = await Task.Run(() => AudioManager.GetNowPlaying(GetMutualGuild()));
            await Context.Channel.SendMessageAsync(nowPlaying);
        }
        [Command("help")]
        [Summary("Displays all commands available and their usages.")]
        public async Task Help()
        {
            await Context.Channel.SendMessageAsync(CommandManager.GetCommandHelp().Result);
        }

        [Command("remove")]
        [Alias("delete")]
        [Summary("Removes the specified track number from the queue (use queue command to get relavent listing)")]
        public async Task RemoveCommand(int trackNumber)
        {
            var guild = GetMutualGuild();
            var voiceChannel = guild.VoiceChannels.Where(x => x.Users.Any(y => y.Id == Context.Message.Author.Id)).FirstOrDefault();
            var response = await AudioManager.RemoveTrackAsync(voiceChannel ,guild, trackNumber);
            await Context.Channel.SendMessageAsync($"{response.Message}");
        }

        [Command("toptracks")]
        [Alias("top")]
        [Summary("Lists top tracks played by the bot with user specified amount (top 10, 20, etc)")]
        public async Task TopTracksCommand([Remainder]int count)
        {
            var tracks = await DatabaseManager.GetTrackHistoriesAsync();
            tracks = tracks.OrderByDescending(x => x.Plays).Take(count).ToList();
            string topTracks = string.Empty;
            for (int i = 0; i < tracks.Count; i++)
            {
                topTracks += $"\n{i + 1}. {tracks[i].Title}\n\tPlays: {tracks[i].Plays}\n";
            }
            await Context.Channel.SendMessageAsync($"Top Tracks:{topTracks}");
        }

        [Command("random")]
        [Alias("rand")]
        [Summary("Plays a random track that has been played before")]
        public async Task RandomTrackCommand()
        {
            var tracks = await DatabaseManager.GetTrackHistoriesAsync();
            var track = tracks[random.Next(tracks.Count)];
            var response = await PlayTrack(track.URL);
            await Context.Channel.SendMessageAsync($"{response.Message}");
        }
    }
}
