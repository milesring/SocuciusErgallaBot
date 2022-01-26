namespace SocuciusErgallaBot.Utility
{
    internal static class SoundtrackInfo
    {
        public static TrackInfo[] Tracks { get; private set; }
        private static Random _random = new Random();

        //morrowind soundtrack info to emulate the bot 'listening' to the actual music
        static SoundtrackInfo()
        {
            Tracks = new TrackInfo[]
            {
                new TrackInfo(){ Title = "Nerevar Rising", Duration = 114000 },
                new TrackInfo(){ Title = "Peaceful Waters", Duration = 185000 },
                new TrackInfo(){ Title = "Knight's Charge", Duration = 124000 },
                new TrackInfo(){ Title = "Over the Next Hill", Duration = 184000 },
                new TrackInfo(){ Title = "Bright Spears Dark Blood", Duration = 126000 },
                new TrackInfo(){ Title = "The Road Most Traveled", Duration = 195000 },
                new TrackInfo(){ Title = "Dance of Swords", Duration = 133000 },
                new TrackInfo(){ Title = "Blessing of Vivec", Duration = 196000 },
                new TrackInfo(){ Title = "Ambush!", Duration = 153000 },
                new TrackInfo(){ Title = "Silt Sunrise", Duration = 191000 },
                new TrackInfo(){ Title = "Hunter's Pursuit", Duration = 137000 },
                new TrackInfo(){ Title = "Shed Your Travails", Duration = 193000 },
                new TrackInfo(){ Title = "Stormclouds on the Battlefield", Duration = 131000 },
                new TrackInfo(){ Title = "Caprice", Duration = 207000 },
                new TrackInfo(){ Title = "Drumbeats of the Dunmer", Duration = 123000 },
                new TrackInfo(){ Title = "Darkened Depths", Duration = 50000 },
                new TrackInfo(){ Title = "The Prophecy Fulfilled", Duration = 71000 },
                new TrackInfo(){ Title = "Triumphant", Duration = 14000 },
                new TrackInfo(){ Title = "Introduction", Duration = 59000 },
                new TrackInfo(){ Title = "Fate's Quickening", Duration = 17000 }
            };
        }

        public static string GetRandomTitle()
        {
            return Tracks[_random.Next(Tracks.Length)].Title;
        }

        public static TrackInfo GetRandomTrack()
        {
            return Tracks[_random.Next(Tracks.Length)];
        }
    }

    internal class TrackInfo
    {
        public string Title { get; set; }
        public int Duration { get; set; }
    }
}
