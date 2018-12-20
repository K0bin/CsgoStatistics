using DemoInfo;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Numerics;
using System.Diagnostics;

namespace CsgoStatistics
{
    public class DemoReader : IDisposable
    {
        private DemoParser parser;
        private Statistics stats;

        private DemoFile file;

        private int demoId;

        private long? defuserSteamId = null;
        private long? planterSteamId = null;
        private long? mvpSteamId = null;

        private Dictionary<long, Vector3> positions = new Dictionary<long, Vector3>();

        private bool hasStarted = false;

        public DemoReader(DemoFile file)
        {
            this.file = file;

            stats = new Statistics();
            demoId = file.FileName.GetHashCode();

            parser = new DemoParser(file.Stream);
            parser.PlayerKilled += Parser_PlayerKilled;
            parser.RoundMVP += Parser_RoundMVP;
            parser.PlayerHurt += Parser_PlayerHurt;
            parser.RoundEnd += Parser_RoundEnd;
            parser.BombDefused += Parser_BombDefused;
            parser.BombPlanted += Parser_BombPlanted;
            parser.RoundStart += Parser_RoundStart;
            parser.TickDone += Parser_TickDone;
            parser.ParseHeader();

            DemoService service = DemoService.Unknown;
            if (parser.Header.ServerName.ToLower().StartsWith("faceit", StringComparison.Ordinal))
                service = DemoService.FaceIt;
            if (parser.Header.ServerName.ToLower().StartsWith("valve", StringComparison.Ordinal))
                service = DemoService.MatchMaking;

            var demo = new Demo
            {
                Id = demoId,
                DateTime = file.LastModified,
                Map = parser.Header.MapName,
                Duration = new TimeSpan(0, 0, (int)(parser.TickTime * parser.Header.PlaybackTicks)),
                Service = service
            };
            stats.Demos.Add(demo);
        }

        private void Parser_TickDone(object sender, TickDoneEventArgs e)
        {
            if (!hasStarted)
                return;

            foreach (var player in parser.Participants)
            {
                if (player == null)
                    continue;

                Vector3 position = new Vector3(player.Position.X, player.Position.Y, player.Position.Z);

                double previousMovement;
                if (!stats.Movement.TryGetValue(player.SteamID, out previousMovement))
                {
                    previousMovement = 0.0;
                }

                Vector3 previousPosition;
                if (positions.TryGetValue(player.SteamID, out previousPosition))
                {
                    stats.Movement[player.SteamID] = previousMovement + (position - previousPosition).Length();
                }
                else
                {
                    stats.Movement[player.SteamID] = 0.0;
                }
                positions[player.SteamID] = position;
            }
        }

        private void Parser_RoundStart(object sender, RoundStartedEventArgs e)
        {
            if (e.TimeLimit < 999)
            {
                hasStarted = true;
            }
        }

        private void Parser_BombPlanted(object sender, BombEventArgs e)
        {
            planterSteamId = e.Player?.SteamID;
        }

        private void Parser_BombDefused(object sender, BombEventArgs e)
        {
            defuserSteamId = e.Player?.SteamID;
        }

        private void Parser_RoundEnd(object sender, RoundEndedEventArgs e)
        {
            stats.Rounds.Add(new Round
            {
                RoundNumber = parser.CTScore + parser.TScore,
                DemoId = demoId,
                DefuserSteamId = defuserSteamId,
                MvpSteamId = mvpSteamId,
                PlanterSteamId = planterSteamId
            });
            defuserSteamId = null;
            planterSteamId = null;
            mvpSteamId = null;
        }

        public Statistics Read()
        {
            parser.ParseToEnd();
            return stats;
        }

        private void Parser_PlayerHurt(object sender, PlayerHurtEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Parser_RoundMVP(object sender, RoundMVPEventArgs e)
        {
            mvpSteamId = e.Player?.SteamID; //can be null for some reason
        }

        private void Parser_PlayerKilled(object sender, PlayerKilledEventArgs e)
        {
            if (!hasStarted || e.Victim == null)
                return; //Kill without victim?!

            stats.Kills.Add(new Kill
            {
                DemoId = demoId,
                Round = parser.CTScore + parser.TScore,
                AssisterSteamId = e.Assister?.SteamID,
                KillerSteamId = e.Killer?.SteamID,
                KillerVelocity = e.Killer != null ? (Vector3?)(new Vector3(e.Killer.Velocity.X, e.Killer.Velocity.Y, e.Killer.Velocity.Z)) : null,
                KillerTeam = e.Killer?.Team,
                PenetratedObjects = e.PenetratedObjects,
                VictimSteamId = e.Victim.SteamID,
                VictimTeam = e.Victim.Team,
                VictimVelocity = new Vector3(e.Victim.Velocity.X, e.Victim.Velocity.Y, e.Victim.Velocity.Z),
                Weapon = e.Weapon
            });
        }

        public void Dispose()
        {
            parser.Dispose();
            file.Dispose();   
        }
    }
}
