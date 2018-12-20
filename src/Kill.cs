using DemoInfo;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace CsgoStatistics
{
    public struct Kill
    {
        public int DemoId;
        public int Round;
        public long? AssisterSteamId;
        public long VictimSteamId;
        public Team? KillerTeam;
        public Vector3? KillerVelocity;
        public long? KillerSteamId;
        public Team VictimTeam;
        public Vector3 VictimVelocity;
        public Equipment Weapon;
        public int PenetratedObjects;
    }
}
