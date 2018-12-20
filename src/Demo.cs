using System;
using System.Collections.Generic;
using System.Text;

namespace CsgoStatistics
{
    public enum DemoService
    {
        Unknown,
        MatchMaking,
        FaceIt
    }

    public enum Map
    {
        Dust2,
        Inferno,
        Overpass,
        Mirage,
        Cache,
        Train,
        Nuke,
        Office,
        Cobblestone,
        Subzero,
        Agency
    }

    public enum Outcome
    {
        Win,
        Tie,
        Loss
    }

    public class Demo
    {
        public int Id;
        public DemoService Service;
        public DateTime DateTime;
        public string Map;
        public Outcome Outcome;
        public TimeSpan Duration;
    }
}
