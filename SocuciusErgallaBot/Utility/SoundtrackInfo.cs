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
                //Morrowind
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
                new TrackInfo(){ Title = "Fate's Quickening", Duration = 17000 },
                //Oblivion
                new TrackInfo(){ Title = "Reign of the Septims", Duration = 110000 },
                new TrackInfo(){ Title = "Through the Valleys", Duration = 259000 },
                new TrackInfo(){ Title = "Death Knell", Duration = 69000 },
                new TrackInfo(){ Title = "Harvest Dawn", Duration = 171000 },
                new TrackInfo(){ Title = "Wind from the Depths", Duration = 102000 },
                new TrackInfo(){ Title = "King and Country", Duration = 244000 },
                new TrackInfo(){ Title = "Fall of the Hammer", Duration = 75000 },
                new TrackInfo(){ Title = "Wings of Kynareth", Duration = 210000 },
                new TrackInfo(){ Title = "Alls Well", Duration = 145000 },
                new TrackInfo(){ Title = "Tension", Duration = 151000 },
                new TrackInfo(){ Title = "March of the Marauders", Duration = 128000 },
                new TrackInfo(){ Title = "Watchman's Ease", Duration = 125000 },
                new TrackInfo(){ Title = "Glory of Cyrodiil", Duration = 148000 },
                new TrackInfo(){ Title = "Defending the Gate", Duration = 81000 },
                new TrackInfo(){ Title = "Bloody Blades", Duration = 74000 },
                new TrackInfo(){ Title = "Minstrel's Lament", Duration = 281000 },
                new TrackInfo(){ Title = "Ancient Sorrow", Duration = 64000 },
                new TrackInfo(){ Title = "Auriel's Ascension", Duration = 185000 },
                new TrackInfo(){ Title = "Daedra in Flight", Duration = 61000 },
                new TrackInfo(){ Title = "Unmarked Stone", Duration = 65000 },
                new TrackInfo(){ Title = "Bloodlust", Duration = 65000 },
                new TrackInfo(){ Title = "Sunrise of Flutes", Duration = 176000 },
                new TrackInfo(){ Title = "Churl's Revenge", Duration = 68000 },
                new TrackInfo(){ Title = "Deep Waters", Duration = 70000 },
                new TrackInfo(){ Title = "Dusk at the Market", Duration = 130000 },
                new TrackInfo(){ Title = "Peace of Akatosh", Duration = 251000 }
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
