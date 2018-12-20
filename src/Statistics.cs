using DemoInfo;
using System;
using System.Collections.Generic;
using System.Text;

namespace CsgoStatistics
{
    public class Statistics
    {
        public List<Demo> Demos
        {
            get; private set;
        } = new List<Demo>();

        public List<Kill> Kills
        {
            get; private set;
        } = new List<Kill>();

        public List<Round> Rounds
        {
            get; private set;
        } = new List<Round>();

        public void AddAll(Statistics statistics)
        {
            Demos.AddRange(statistics.Demos);
            Kills.AddRange(statistics.Kills);
            Rounds.AddRange(statistics.Rounds);
        }
    }
}
