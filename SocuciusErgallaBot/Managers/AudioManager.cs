using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;

namespace SocuciusErgallaBot.Managers
{
    internal static class AudioManager
    {
        private static readonly LavaNode _lavaNode = ServiceManager.Provider.GetRequiredService<LavaNode>();
        private static DiscordSocketClient _client = ServiceManager.GetService<DiscordSocketClient>();
        private static readonly int _defaultVolume = 10;

        private static List<QueuedTrackInfo> _queuedTracks = new();
        static AudioManager()
        {
            _lavaNode.OnTrackEnded += _lavaNode_OnTrackEnded;
            _lavaNode.OnTrackStarted += _lavaNode_OnTrackStarted;
        }

        private static async Task _lavaNode_OnTrackStarted(TrackStartEventArgs arg)
        {
            await _client.SetGameAsync($"{arg.Track.Title} - {arg.Track.Author}", null, Discord.ActivityType.Listening);
        }

        private static async Task _lavaNode_OnTrackEnded(TrackEndedEventArgs arg)
        {
            if (arg.Reason != TrackEndReason.Finished) return;

            //queue is now empty
            if (!arg.Player.Queue.TryDequeue(out var queueable))
            {
                await arg.Player.StopAsync();
                await EventManager.SetNowPlayingTimer();
                return;
            }


            if (!(queueable is LavaTrack track))
            {
                return;
            }
            var trackInfo = _queuedTracks[0];
            _queuedTracks.RemoveAt(0);
            if (trackInfo?.Track != queueable)
            {
                return;
            }
            await arg.Player.PlayAsync(track);
            await arg.Player.SeekAsync(trackInfo.StartTime);
        }

        public static async Task<MusicResponse> JoinAsync(IGuild guild, IVoiceChannel voiceChannel, ITextChannel channel)
        {
            if (_lavaNode.HasPlayer(guild))
            {
                var response = new MusicResponse()
                {
                    Message = "Player already connected to voice channel",
                    Status = MusicResponseStatus.Valid
                };

                var player = _lavaNode.GetPlayer(guild);
                if(player.VoiceChannel.Id == voiceChannel.Id)
                {
                    return response;
                }
                var users = await player.VoiceChannel.GetUsersAsync(CacheMode.CacheOnly, new RequestOptions { AuditLogReason = "Bot checking for user count in current voice channel."}).ToListAsync();
                if(users.Count == 1)
                {
                    await LeaveAsync(player.VoiceChannel, guild);
                    //leave channel
                }
                
            }
            if (voiceChannel is null) return new MusicResponse()
            {
                Message = "You must be connected to a voice channel",
                Status = MusicResponseStatus.Error
            };
            try
            {
                await _lavaNode.JoinAsync(voiceChannel, channel);
                var player = _lavaNode.GetPlayer(guild);
                await player.UpdateVolumeAsync((ushort)_defaultVolume);
                return new MusicResponse()
                {
                    Message = $"Joined {voiceChannel.Name}",
                    Status = MusicResponseStatus.Valid
                };
            }
            catch (Exception ex)
            {
                return new MusicResponse()
                {
                    Message = $"ERROR\t{ex.Message}",
                    Status = MusicResponseStatus.Error
                };
            }

        }

        public static async Task<MusicResponse> PlayAsync(IVoiceChannel voiceChannel, IGuild guild, string query)
        {
            //check if caller is in voice channel
            if (voiceChannel is null)
                return new MusicResponse()
                {
                    Message = "You must join a voice channel",
                    Status = MusicResponseStatus.Error
                };

            //check if player is connected to voice channel
            if (!_lavaNode.HasPlayer(guild))
                return new MusicResponse()
                {
                    Message = "I'm not connected to the voice channel",
                    Status = MusicResponseStatus.Error
                };

            try
            {
                var player = _lavaNode.GetPlayer(guild);
                if (!CheckIfUserInSameVoiceChannel(voiceChannel, player.VoiceChannel))
                    return new MusicResponse()
                    {
                        Message = "You must be in the same voice channel to command the bot.",
                        Status = MusicResponseStatus.Error
                    };
                LavaTrack track;
                var search = Uri.IsWellFormedUriString(query, UriKind.Absolute)
                    ? await _lavaNode.SearchAsync(Victoria.Responses.Search.SearchType.Direct, query)
                    : await _lavaNode.SearchYouTubeAsync(query);

                if (search.Status == Victoria.Responses.Search.SearchStatus.NoMatches) return new MusicResponse()
                {
                    Message = $"I could not locate anything for {query}",
                    Status = MusicResponseStatus.Error
                };

                track = search.Tracks.FirstOrDefault();

                //check if youtube link has timestamp
                Regex rx = new(@"t=(\d*)\w*$", RegexOptions.Compiled);
                var match = rx.Match(query);
                TimeSpan startTime = TimeSpan.Zero;
                if (match.Success)
                {
                    startTime = TimeSpan.FromSeconds(Convert.ToInt32(match.Groups[1].Value));
                }


                //add to queue
                if (player.Track != null
                    && player.PlayerState is PlayerState.Playing
                    || player.PlayerState is PlayerState.Paused)
                {
                    player.Queue.Enqueue(track);
                    var newTrackInfo = new QueuedTrackInfo()
                    {
                        Track = track,
                        StartTime = startTime
                    };
                    _queuedTracks.Add(newTrackInfo);
                    Console.WriteLine($"({DateTime.Now}\t(AUDIO)\tTrack was added to queue");
                    return new MusicResponse()
                    {
                        Message = $"{track.Title} - {track.Author} has been added to queue",
                        Status = MusicResponseStatus.Valid
                    };
                }

                //play song
                await player.PlayAsync(track);
                //seek to starttime, zero by default
                await player.SeekAsync(startTime);
                return new MusicResponse()
                {
                    Message = $"{track.Title} - {track.Author} at {startTime:c}",
                    Status = MusicResponseStatus.Valid
                };

            }
            catch (Exception ex)
            {
                return new MusicResponse()
                {
                    Message = ex.Message,
                    Status = MusicResponseStatus.Error
                };
            }
        }

        public static async Task<MusicResponse> PlayNextAsync(IVoiceChannel voiceChannel, IGuild guild)
        {
            if (voiceChannel is null) return new MusicResponse()
            {
                Message = "You must join a voice channel",
                Status = MusicResponseStatus.Error
            };

            if (!_lavaNode.HasPlayer(guild)) return new MusicResponse()
            {
                Message = "I'm not connected to the voice channel",
                Status = MusicResponseStatus.Error
            };

            var player = _lavaNode.GetPlayer(guild);

            if (!CheckIfUserInSameVoiceChannel(voiceChannel, player.VoiceChannel))
                return new MusicResponse()
                {
                    Message = "You must be in the same voice channel to command the bot.",
                    Status = MusicResponseStatus.Error
                };

            if (player.Queue.Count == 0)
            {
                return new MusicResponse()
                {
                    Message = $"No further tracks to skip to",
                    Status = MusicResponseStatus.Error
                };
            }
            player.Queue.TryDequeue(out var nextTrack);
            var nextTrackInfo = _queuedTracks[0];
            _queuedTracks.RemoveAt(0);

            await player.PlayAsync(nextTrack);
            await player.SeekAsync(nextTrackInfo?.StartTime);
            return new MusicResponse()
            {
                Message = $"{nextTrack.Title} - {nextTrack.Author}",
                Status = MusicResponseStatus.Valid
            };
        }

        public static async Task<MusicResponse> LeaveAsync(IVoiceChannel voiceChannel, IGuild guild)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);
                if(!CheckIfUserInSameVoiceChannel(voiceChannel, player.VoiceChannel))
                    return new MusicResponse()
                    {
                        Message = "You must be in the same voice channel to command the bot.",
                        Status = MusicResponseStatus.Error
                    };

                if (player.PlayerState is PlayerState.Playing) 
                    await player.StopAsync();

                await _lavaNode.LeaveAsync(player.VoiceChannel);
                Console.WriteLine($"{DateTime.Now}\t(AUDIO)\tBot left a voice channel.");
                _queuedTracks.Clear();
                return new MusicResponse()
                {
                    Message = "I have left the voice channel",
                    Status = MusicResponseStatus.Valid
                };
            }
            catch (InvalidOperationException ex)
            {
                return new MusicResponse()
                {
                    Message = ex.Message,
                    Status = MusicResponseStatus.Error
                };
            }
        }

        

        public static async Task<MusicResponse> SetVolumeAsync(IVoiceChannel voiceChannel, IGuild guild, int volume)
        {
            int min = 0, max = 150;
            if (volume > max || volume <= min) return new MusicResponse()
            {
                Message = $"Volume must be between {min} and {max}",
                Status = MusicResponseStatus.Error
            };

            try
            {
                var player = _lavaNode.GetPlayer(guild);

                if (!CheckIfUserInSameVoiceChannel(voiceChannel, player.VoiceChannel))
                    return new MusicResponse()
                    {
                        Message = "You must be in the same voice channel to command the bot.",
                        Status = MusicResponseStatus.Error
                    };

                await player.UpdateVolumeAsync((ushort)volume);
                return new MusicResponse()
                {
                    Message = $"Volume has been set to {volume}",
                    Status = MusicResponseStatus.Valid
                };
            }
            catch (Exception ex)
            {
                return new MusicResponse()
                {
                    Message = ex.Message,
                    Status = MusicResponseStatus.Error
                };
            }
        }

        public static async Task<MusicResponse> TogglePauseAsync(IVoiceChannel voiceChannel, IGuild guild)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);
                if (!CheckIfUserInSameVoiceChannel(voiceChannel, player.VoiceChannel))
                    return new MusicResponse()
                    {
                        Message = "You must be in the same voice channel to command the bot.",
                        Status = MusicResponseStatus.Error
                    };

                if (player.PlayerState == PlayerState.Playing)
                {
                    await player.PauseAsync();
                    return new MusicResponse()
                    {
                        Message = $"Paused: {player.Track.Title} - {player.Track.Author}",
                        Status = MusicResponseStatus.Valid
                    };
                }

                if (player.PlayerState == PlayerState.Paused)
                {
                    await player.ResumeAsync();
                    return new MusicResponse()
                    {
                        Message = $"Resumed: {player.Track.Title} - {player.Track.Author}",
                        Status = MusicResponseStatus.Valid
                    };
                }
            }
            catch (InvalidOperationException ex)
            {
                return new MusicResponse()
                {
                    Message = ex.Message,
                    Status = MusicResponseStatus.Error
                };
            }

            return new MusicResponse()
            {
                Message = $"There is nothing to pause",
                Status = MusicResponseStatus.Valid
            };
        }

        public static async Task<MusicResponse> RemoveTrackAsync(IVoiceChannel voiceChannel, IGuild guild, int trackNumber)
        {
            //convert to 0 based index
            trackNumber--;

            //check if tracknumber is valid range
            if (trackNumber < 0 || trackNumber > _queuedTracks.Count)
            {
                return new MusicResponse()
                {
                    Message = "Track number to be removed not valid",
                    Status = MusicResponseStatus.Error
                };
            }

            //check if player is connected to voice channel
            if (!_lavaNode.HasPlayer(guild)) return new MusicResponse()
            {
                Message = "Player is not connected to a voice channel",
                Status = MusicResponseStatus.Error
            };

            try
            {
                var player = _lavaNode.GetPlayer(guild);

                if (!CheckIfUserInSameVoiceChannel(voiceChannel, player.VoiceChannel))
                    return new MusicResponse()
                    {
                        Message = "You must be in the same voice channel to command the bot.",
                        Status = MusicResponseStatus.Error
                    };

                player.Queue.RemoveAt(trackNumber);
                _queuedTracks.RemoveAt(trackNumber);
                return new MusicResponse()
                {
                    Message = $"Track {trackNumber} successfully removed",
                    Status = MusicResponseStatus.Valid
                };

            }
            catch (Exception ex)
            {
                return new MusicResponse()
                {
                    Message = ex.Message,
                    Status = MusicResponseStatus.Error
                };
            }
        }

        public static Task<string> GetQueue(IGuild guild)
        {
            try
            {
                LavaPlayer player = _lavaNode.GetPlayer(guild);
            }
            catch (Exception)
            {
                return Task.FromResult("Nothing currently playing");
            }

            var queueToList = _queuedTracks.ToList();
            var queueString = GetNowPlaying(guild).Result + "\n";
            for (int i = 0; i < queueToList.Count; i++)
            {
                var track = queueToList[i];
                queueString += $"{i + 1}: {track.Track.Title} - {track.Track.Author}\n\tStart Time:{track.StartTime}\n\t{track.Track.Url}\n";
                if (i != queueToList.Count - 1)
                {
                    queueString += "\n";
                }
            }
            return Task.FromResult(queueString);
        }

        public static Task<string> GetNowPlaying(IGuild guild)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);
                var track = player.Track;
                if (track == null)
                {
                    return Task.FromResult("Nothing currently playing.");
                }
                return Task.FromResult($"\nNow playing: {track.Title} - {track.Author}\n\t{track.Position} - {track.Duration}\n\t{track.Url}");
            }
            catch (Exception)
            {
                return Task.FromResult($"Nothing currently playing.");
            }
        }

        public static Task<IVoiceChannel> GetCurrentChannel(IGuild guild)
        {
            var player = _lavaNode.GetPlayer(guild);
            return Task.FromResult(player.VoiceChannel);
        }

        private static bool CheckIfUserInSameVoiceChannel(IVoiceChannel userChannel, IVoiceChannel botChannel)
        {
            return botChannel.Id == userChannel.Id;
        }
    }

    public class MusicResponse
    {
        public string Message { get; set; }
        public MusicResponseStatus Status { get; set; }
    }

    public enum MusicResponseStatus
    {
        Valid,
        Error
    }

    public class QueuedTrackInfo
    {
        public LavaTrack Track { get; set; }
        public TimeSpan StartTime { get; set; }
    }

}
