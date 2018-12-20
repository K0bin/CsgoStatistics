using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Numerics;

namespace CsgoStatistics
{
    public class CsgoAnalyzer
    {
        private List<Thread> parserThreads = new List<Thread>();
        private List<Thread> loaderThreads = new List<Thread>();
        private Queue<string> filesQueue = new Queue<string>();
        private Queue<DemoFile> loadedFiles = new Queue<DemoFile>();
        private List<Statistics> demos = new List<Statistics>();

        private int remainingDemosCount = 0;

        public CsgoAnalyzer(string folder, int count = 0)
        {
            var files = Directory
                .EnumerateFiles(folder)
                .Where(f => f.EndsWith(".dem", StringComparison.InvariantCulture));

            if (count == 2018)
            {
                files = files.Where(f => File.GetLastWriteTimeUtc(f).Year == 2018);
            }
            else if (count == 2017)
            {
                files = files.Where(f => File.GetLastWriteTimeUtc(f).Year == 2017);
            }
            else if (count > 0)
            {
                files = files.Take(count);
            }

            files = files
                .OrderByDescending(f => (File.GetLastWriteTimeUtc(f) - new DateTime(1970, 1, 1)).TotalSeconds);

            filesQueue = new Queue<string>(files.ToList());

            for (var i = 0; i < Environment.ProcessorCount; i++)
            {
                var thread = new Thread(ParserThreadStart);
                thread.Priority = ThreadPriority.Highest;
                parserThreads.Add(thread);
            }
            for (var i = 0; i < Math.Max(Environment.ProcessorCount / 4, 2); i++)
            {
                var thread = new Thread(LoaderThreadStart);
                thread.Priority = ThreadPriority.Lowest;
                loaderThreads.Add(thread);
            }
        }

        private void LoaderThreadStart()
        {
            while (true)
            {
                string path = null;
                lock (filesQueue)
                {
                    if (filesQueue.Count != 0)
                        path = filesQueue.Dequeue();
                    else
                        break;
                }

                var watch = new Stopwatch();
                watch.Start();
                var demoFile = new DemoFile(path);
                watch.Stop();
                Console.WriteLine($"[LOAD] Demo: {demoFile.FileName} took: {watch.ElapsedMilliseconds} ms");
                int loadedFilesCount = 0;
                lock (loadedFiles)
                {
                    loadedFilesCount = loadedFiles.Count;
                    loadedFiles.Enqueue(demoFile);
                }
                if (loadedFilesCount > parserThreads.Count)
                {
                    //Give the parser threads time to catch up
                    Thread.Sleep(1000);
                }
            }
        }

        private void ParserThreadStart()
        {
            while (true)
            {
                DemoFile file = null;
                lock (loadedFiles)
                {
                    if (loadedFiles.Count != 0)
                        file = loadedFiles.Dequeue();
                    else if (remainingDemosCount == 0)
                        break;
                       
                }
                if (file != null)
                {
                    var watch = new Stopwatch();
                    Statistics demo = null;
                    try
                    {
                        using (var reader = new DemoReader(file))
                        {
                            watch.Start();
                            demo = reader.Read();
                            watch.Stop();
                        }
                    }
                    catch (NotImplementedException e)
                    {
                        Console.WriteLine("[CORRUPT] Demo is either corrupted or incompatible with demoinfo library");
                    }

                    if (demo != null)
                    {
                        Console.WriteLine($"[PARSE] Demo: {file.FileName} took: {watch.ElapsedMilliseconds} ms");
                        lock (demos)
                        {
                            demos.Add(demo);
                        }
                    }
                    Interlocked.Decrement(ref remainingDemosCount);
                    Console.WriteLine($"[PROGRESS] {remainingDemosCount} demos left");
                }
                else
                {
                    //Give the loader threads time to catch up
                    Thread.Sleep(250);
                }
            }
        }

        public void Analyze()
        {
            remainingDemosCount = filesQueue.Count;
            demos.Capacity = remainingDemosCount;
            foreach (var thread in parserThreads)
            {
                thread.Start();
            }
            foreach (var thread in loaderThreads)
            {
                thread.Start();
            }

            while (true)
            {
                if (remainingDemosCount == 0)
                    break;

                Thread.Sleep(500);
            }

            var combined = new Statistics();
            demos.ForEach(combined.AddAll);
            demos.Clear();

            var players = Enum.GetValues(typeof(Players));

            var ferrari = combined
                .Kills
                .Where(k =>
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (k.VictimSteamId == (long)players.GetValue(i))
                            return true;
                    }
                    return false;
                })
                .GroupBy(k => k.VictimSteamId)
                .Select(k => Tuple.Create(k.Key, k.Average(s => s.KillerVelocity?.Length() ?? 0)))
                .OrderBy(tuple => tuple.Item2)
                .OrderByDescending(t => t.Item2)
                .ToList();

            var aces = combined
                .Kills
                .Where(k =>
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (k.KillerSteamId == (long)players.GetValue(i))
                            return true;
                    }
                    return false;
                })
                .GroupBy(k => Tuple.Create(k.DemoId, k.Round, k.KillerSteamId))
                .Select(g => Tuple.Create(g.Key.Item3, g.Count(k => k.VictimTeam != k.KillerTeam)))
                .Where(t => t.Item2 == 5)
                .GroupBy(t => t.Item1)
                .Select(g => Tuple.Create(g.Key, g.Count()))
                .OrderByDescending(t => t.Item2)
                .ToList();

            var plants = combined
                .Rounds
                .Where(r =>
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (r.PlanterSteamId == (long)players.GetValue(i))
                            return true;
                    }
                    return false;
                })
                .GroupBy(r => r.PlanterSteamId)
                .Select(g => Tuple.Create(g.Key, g.Count()))
                .OrderByDescending(t => t.Item2)
                .ToList();

            var defuses = combined
                .Rounds
                .Where(r =>
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (r.DefuserSteamId == (long)players.GetValue(i))
                            return true;
                    }
                    return false;
                })
                .GroupBy(r => r.DefuserSteamId)
                .Select(g => Tuple.Create(g.Key, g.Count()))
                .OrderByDescending(t => t.Item2)
                .ToList();

            var movement = combined
                .Movement
                .Where(m =>
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (m.Key == (long)players.GetValue(i))
                            return true;
                    }
                    return false;
                })
                .OrderByDescending(t => t.Value)
                .ToList();



            Console.WriteLine();
            Console.WriteLine("=======================================");
            Console.WriteLine("Ferrari Peeks");
            ferrari.ForEach(f => Console.WriteLine($"{Enum.GetName(typeof(Players), f.Item1)}: {UnitToKm(f.Item2) * 3.6f} km/h"));

            Console.WriteLine();
            Console.WriteLine("=======================================");
            Console.WriteLine("Aces");
            aces.ForEach(f => Console.WriteLine($"{Enum.GetName(typeof(Players), f.Item1)}: {f.Item2}"));

            Console.WriteLine();
            Console.WriteLine("=======================================");
            Console.WriteLine("Plants");
            plants.ForEach(f => Console.WriteLine($"{Enum.GetName(typeof(Players), f.Item1)}: {f.Item2}"));

            Console.WriteLine();
            Console.WriteLine("=======================================");
            Console.WriteLine("Defuses");
            defuses.ForEach(f => Console.WriteLine($"{Enum.GetName(typeof(Players), f.Item1)}: {f.Item2}"));

            Console.WriteLine();
            Console.WriteLine("=======================================");
            Console.WriteLine("Movement");
            movement.ForEach(m => Console.WriteLine($"{Enum.GetName(typeof(Players), m.Key)}: {UnitToKm(m.Value)}"));

            Console.ReadKey();
        }

        private double UnitToKm(double units)
        {
            return units * 30.48f / 16 / 100;
        }
    }
}
