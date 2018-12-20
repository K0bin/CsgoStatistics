using System;
using System.Collections.Generic;
using System.Text;

namespace CsgoStatistics
{
    public class Player
    {
        public long SteamId
        {
            get; private set;
        }

        public int Kills { get; set; }

        public int Assists { get; set; }

        public int Deaths { get; set; }

        public int Mvps { get; set; }

        public Player(long steamId)
        {
            this.SteamId = steamId;
        }
    }
}
