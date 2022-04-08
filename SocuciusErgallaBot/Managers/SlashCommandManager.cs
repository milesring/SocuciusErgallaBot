using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace SocuciusErgallaBot.Managers
{
    internal static class SlashCommandManager
    {
        private static Random random = new Random();
        private static DiscordSocketClient _client = ServiceManager.GetService<DiscordSocketClient>();
        public static async Task LoadSlashCommands(ulong guildId)
        {
            var guild = _client.GetGuild(guildId);
            List<ApplicationCommandProperties> applicationCommandProperties = new();
            applicationCommandProperties.Add(BuildPlayCommand());
            applicationCommandProperties.Add(BuildStopCommand());
            applicationCommandProperties.Add(BuildVolumeCommand());
            applicationCommandProperties.Add(BuildSkipCommand());
            applicationCommandProperties.Add(BuildPauseCommand());
            applicationCommandProperties.Add(BuildQueueCommand());
            applicationCommandProperties.Add(BuildNowPlayingCommand());
            applicationCommandProperties.Add(BuildRemoveCommand());
            applicationCommandProperties.Add(BuildTopCommand());
            applicationCommandProperties.Add(BuildPlayRandomCommand());
            applicationCommandProperties.Add(BuildSeekCommand());

            try
            {
                await guild.DeleteApplicationCommandsAsync();
                await guild.BulkOverwriteApplicationCommandAsync(applicationCommandProperties.ToArray());
            }
            catch (HttpException ex)
            {
                Console.WriteLine($"Error in creating slash commands.\n{ex.Message})");
            }
        }

        private static ApplicationCommandProperties BuildPlayCommand()
        {
            var commandProperties = new SlashCommandBuilder();
            commandProperties.WithName("play")
                .WithDescription("Plays audio from youtube using a url or search terms.")
                .AddOption("query", ApplicationCommandOptionType.String, "The query or URL to be played.", isRequired: true);
            return commandProperties.Build();
        }
        private static ApplicationCommandProperties BuildStopCommand()
        {
            var commandProperties = new SlashCommandBuilder();
            commandProperties.WithName("stop")
                .WithDescription("Stops playback and leaves the voice channel.");
            return commandProperties.Build();
        }
        private static ApplicationCommandProperties BuildVolumeCommand()
        {
            var commandProperties = new SlashCommandBuilder();
            commandProperties.WithName("volume")
                .WithDescription("Changes the bot's volume between 0 and 100. Default value is 10.")
                .AddOption("amount", ApplicationCommandOptionType.Integer, "Volume level to be changed to.", isRequired: true, minValue: 0, maxValue: 100);
            return commandProperties.Build();
        }
        private static ApplicationCommandProperties BuildSkipCommand()
        {
            var commandProperties = new SlashCommandBuilder();
            commandProperties.WithName("skip")
                .WithDescription("Changes track to next in queue.");
            return commandProperties.Build();
        }
        private static ApplicationCommandProperties BuildPauseCommand()
        {
            var commandProperties = new SlashCommandBuilder();
            commandProperties.WithName("pause")
                .WithDescription("Toggles the player between pause and playing state.");
            return commandProperties.Build();
        }
        private static ApplicationCommandProperties BuildQueueCommand()
        {
            var commandProperties = new SlashCommandBuilder();
            commandProperties.WithName("queue")
                .WithDescription("Displays the current queue of the player.");
            return commandProperties.Build();
        }
        private static ApplicationCommandProperties BuildNowPlayingCommand()
        {
            var commandProperties = new SlashCommandBuilder();
            commandProperties.WithName("nowplaying")
                .WithDescription("Displays the current playing song.");
            return commandProperties.Build();
        }
        private static ApplicationCommandProperties BuildRemoveCommand()
        {
            var commandProperties = new SlashCommandBuilder();
            commandProperties.WithName("remove")
                .WithDescription("Removes the specified track number from the queue. (Use queue command to get number.)")
                .AddOption("track", ApplicationCommandOptionType.Integer, "The number of the track to be removed.", isRequired: true);
            return commandProperties.Build();
        }
        private static ApplicationCommandProperties BuildTopCommand()
        {
            var commandProperties = new SlashCommandBuilder();
            commandProperties.WithName("top")
                .WithDescription("Lists top tracks played by the bot with user specified amount.")
                .AddOption("number", ApplicationCommandOptionType.Integer, "The number of the track to be displayed.", isRequired: true, minValue: 0, maxValue: 100);
            return commandProperties.Build();
        }
        private static ApplicationCommandProperties BuildPlayRandomCommand()
        {
            var commandProperties = new SlashCommandBuilder();
            commandProperties.WithName("playrandom")
                .WithDescription("Plays a random track that has been played before.")
                .AddOption("amount", ApplicationCommandOptionType.Integer, "The number of random tracks to add to the queue.", isRequired: false, minValue: 1, maxValue: 20);
            return commandProperties.Build();
        }

        private static ApplicationCommandProperties BuildSeekCommand()
        {
            var commandProperties = new SlashCommandBuilder();
            commandProperties.WithName("seek")
                .WithDescription("Seeks to a location in the currently playing song.")
                .AddOption("percentage", ApplicationCommandOptionType.Number, "The location of the song to seek to using a percentage of the total song.", isRequired: true, minValue: 0, maxValue: 100);
            return commandProperties.Build();
        }


        public static async Task HandleSlashCommand(SocketSlashCommand command)
        {
            await command.DeferAsync(ephemeral: true);
            switch (command.Data.Name)
            {
                case "play":
                    await HandlePlayCommand(command).ConfigureAwait(false);
                    break;
                case "stop":
                    await HandleStopCommand(command).ConfigureAwait(false);
                    break;
                case "volume":
                    await HandleVolumeCommand(command).ConfigureAwait(false);
                    break;
                case "skip":
                    await HandleSkipCommand(command).ConfigureAwait(false);
                    break;
                case "pause":
                    await HandlePauseCommand(command).ConfigureAwait(false);
                    break;
                case "queue":
                    await HandleQueueCommand(command).ConfigureAwait(false);
                    break;
                case "nowplaying":
                    await HandleNowPlayingCommand(command).ConfigureAwait(false);
                    break;
                case "remove":
                    await HandleRemoveCommand(command).ConfigureAwait(false);
                    break;
                case "top":
                    await HandleTopCommand(command).ConfigureAwait(false);
                    break;
                case "playrandom":
                    await HandlePlayRandomCommand(command).ConfigureAwait(false);
                    break;
                case "seek":
                    await HandleSeekCommand(command).ConfigureAwait(false);
                    break;
            }
        }

        private static async Task HandlePlayCommand(SocketSlashCommand command)
        {
            var user = (SocketGuildUser)command.User;
            var guild = user.Guild;
            SocketVoiceChannel? voiceChannel = GetVoiceChannel(user, guild);
            var response = await AudioManager.JoinAsync(voiceChannel, guild);
            if (response.Status == MusicResponseStatus.Error)
            {
                var embedBuilder = new EmbedBuilder()
                    .WithTitle(nameof(MusicResponseStatus.Error))
                    .WithDescription(response.Message)
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
                return;
            }
            response = await AudioManager.PlayAsync(voiceChannel, guild, (string)command.Data.Options.First().Value, user);
            if (response.Status != MusicResponseStatus.Error)
            {
                EventManager.StopNowPlayingTimer();
                var embedBuilder = new EmbedBuilder()
                    .WithDescription(response.Message)
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
            }
            else
            {
                var embedBuilder = new EmbedBuilder()
                    .WithDescription(response.Message)
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
            }
        }

        private static SocketVoiceChannel? GetVoiceChannel(SocketGuildUser user, SocketGuild guild)
        {
            return guild.VoiceChannels.Where(x => x.Users.Any(y => y.Id == user.Id)).FirstOrDefault();
        }

        private static async Task HandleStopCommand(SocketSlashCommand command)
        {
            var user = (SocketGuildUser)command.User;
            var guild = user.Guild;
            var voiceChannel = GetVoiceChannel(user, guild);
            var response = await AudioManager.LeaveAsync(voiceChannel, guild);
            if (response.Status == MusicResponseStatus.Valid)
            {
                await EventManager.SetNowPlayingTimer();
                var embedBuilder = new EmbedBuilder()
                    .WithDescription(response.Message)
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
            }
            else
            {
                await EventManager.SetNowPlayingTimer();
                var embedBuilder = new EmbedBuilder()
                    .WithTitle(nameof(MusicResponseStatus.Error))
                    .WithDescription(response.Message)
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
            }
        }

        private static async Task HandleVolumeCommand(SocketSlashCommand command)
        {
            var user = (SocketGuildUser)command.User;
            var guild = user.Guild;
            var voiceChannel = GetVoiceChannel(user, guild);
            var response = await AudioManager.SetVolumeAsync(voiceChannel, guild, Convert.ToInt32(command.Data.Options.First().Value));
            if (response.Status == MusicResponseStatus.Valid)
            {
                var embedBuilder = new EmbedBuilder()
                    .WithDescription(response.Message)
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
            }
            else
            {
                var embedBuilder = new EmbedBuilder()
                    .WithTitle(nameof(MusicResponseStatus.Error))
                    .WithDescription(response.Message)
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
            }
        }

        private static async Task HandleSkipCommand(SocketSlashCommand command)
        {
            var user = (SocketGuildUser)command.User;
            var guild = user.Guild;
            var voiceChannel = GetVoiceChannel(user, guild);
            var response = await AudioManager.PlayNextAsync(voiceChannel, guild);
            if (response.Status == MusicResponseStatus.Valid)
            {
                var embedBuilder = new EmbedBuilder()
                    .WithDescription(response.Message)
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
            }
            else
            {
                var embedBuilder = new EmbedBuilder()
                    .WithTitle(nameof(MusicResponseStatus.Error))
                    .WithDescription(response.Message)
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
            }
        }

        private static async Task HandlePauseCommand(SocketSlashCommand command)
        {
            var user = (SocketGuildUser)command.User;
            var guild = user.Guild;
            var voiceChannel = GetVoiceChannel(user, guild);
            var response = await AudioManager.TogglePauseAsync(voiceChannel, guild);
            if (response.Status == MusicResponseStatus.Valid)
            {
                var embedBuilder = new EmbedBuilder()
                    .WithDescription(response.Message)
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
            }
            else
            {
                var embedBuilder = new EmbedBuilder()
                    .WithTitle(nameof(MusicResponseStatus.Error))
                    .WithDescription(response.Message)
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
            }
        }

        private static async Task HandleQueueCommand(SocketSlashCommand command)
        {
            var user = (SocketGuildUser)command.User;
            var guild = user.Guild;
            var response = await Task.Run(() => AudioManager.GetQueue(guild));
            var embedBuilder = new EmbedBuilder()
                    .WithDescription(response)
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp();
            await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
        }

        private static async Task HandleNowPlayingCommand(SocketSlashCommand command)
        {
            var user = (SocketGuildUser)command.User;
            var guild = user.Guild;
            var response = await Task.Run(() => AudioManager.GetNowPlaying(guild));
            var embedBuilder = new EmbedBuilder()
                    .WithDescription(response)
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp();
            await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
        }

        private static async Task HandleRemoveCommand(SocketSlashCommand command)
        {
            var user = (SocketGuildUser)command.User;
            var guild = user.Guild;
            var voiceChannel = GetVoiceChannel(user, guild);
            var response = await AudioManager.RemoveTrackAsync(voiceChannel, guild, Convert.ToInt32(command.Data.Options.First().Value));
            if (response.Status == MusicResponseStatus.Valid)
            {
                var embedBuilder = new EmbedBuilder()
                    .WithDescription(response.Message)
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
            }
            else
            {
                var embedBuilder = new EmbedBuilder()
                    .WithTitle(nameof(MusicResponseStatus.Error))
                    .WithDescription(response.Message)
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
            }
        }

        private static async Task HandleTopCommand(SocketSlashCommand command)
        {
            var tracks = await DatabaseManager.GetTrackHistoriesAsync();
            tracks = tracks.OrderByDescending(x => x.Plays).Take(Convert.ToInt32(command.Data.Options.First().Value)).ToList();
            string topTracks = string.Empty;
            for (int i = 0; i < tracks.Count; i++)
            {
                topTracks += $"\n{i + 1}. {tracks[i].Title}\n\tPlays: {tracks[i].Plays}\n";
            }
            var embedBuilder = new EmbedBuilder()
                    .WithDescription($"Top Tracks: {topTracks}")
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp();
            await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
        }

        private static async Task HandlePlayRandomCommand(SocketSlashCommand command)
        {

            var user = (SocketGuildUser)command.User;
            var guild = user.Guild;
            var voiceChannel = GetVoiceChannel(user, guild);
            var response = await AudioManager.JoinAsync(voiceChannel, guild);
            if (response.Status == MusicResponseStatus.Error)
            {
                var embedBuilder = new EmbedBuilder()
                    .WithTitle(nameof(MusicResponseStatus.Error))
                    .WithDescription(response.Message)
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
                return;
            }

            var success = int.TryParse(command.Data.Options.FirstOrDefault()?.Value.ToString(), out int numTracks);
            numTracks = success ? numTracks : 1;
            var trackQuery = await DatabaseManager.GetTrackHistoriesAsync();
            var tracks = new List<Models.TrackHistory>(trackQuery);
            List<MusicResponse> responseList = new();
            for (int i = 0; i < numTracks; i++)
            {
                var track = tracks[random.Next(tracks.Count)];
                tracks.Remove(track);
                var randomPlayResponse = await AudioManager.PlayAsync(voiceChannel, guild, track.URL, user);
                responseList.Add(randomPlayResponse);
            }

            if (responseList.All(x => x.Status == MusicResponseStatus.Valid))
            {
                var responseMessage = string.Empty;

                foreach(var successResponse in responseList)
                {
                    responseMessage += $"{responseList.IndexOf(successResponse)+1}. - {successResponse.Message}\n\n";
                }
                EventManager.StopNowPlayingTimer();
                var embedBuilder = new EmbedBuilder()
                .WithDescription(responseMessage)
                .WithColor(Color.Green)
                .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
            }
            else
            {
                var failedResponses = responseList.Where(x => x.Status == MusicResponseStatus.Error).ToList();
                var responseMessage = string.Empty;
                foreach (var failedResponse in failedResponses)
                {
                    responseMessage += $"{failedResponses.IndexOf(failedResponse)+1}. - {failedResponse.Message}\n\n";
                }
                var embedBuilder = new EmbedBuilder()
                .WithTitle(nameof(MusicResponseStatus.Error))
                .WithDescription(responseMessage)
                .WithColor(Color.Red)
                .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
                return;
            }
        }

        private static async Task HandleSeekCommand(SocketSlashCommand command)
        {
            var user = (SocketGuildUser)command.User;
            var guild = user.Guild;
            var voiceChannel = GetVoiceChannel(user, guild);
            var response = await AudioManager.SeekAsync(voiceChannel, guild, Convert.ToInt32(command.Data.Options.First().Value));
            if (response.Status == MusicResponseStatus.Valid)
            {
                var embedBuilder = new EmbedBuilder()
                    .WithDescription(response.Message)
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
            }
            else
            {
                var embedBuilder = new EmbedBuilder()
                    .WithTitle(nameof(MusicResponseStatus.Error))
                    .WithDescription(response.Message)
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();
                await command.ModifyOriginalResponseAsync((prop) => prop.Embed = embedBuilder.Build());
            }
        }
    }
}
