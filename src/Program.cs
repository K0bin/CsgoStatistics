using System;

namespace CsgoStatistics
{
    class Program
    {
        static void Main(string[] args)
        {
            var demoCount = 0;
            if (args.Length > 0)
            {
                if (!int.TryParse(args[0], out demoCount))
                    demoCount = 0;
            }
            Console.WriteLine("Csgo Analysis");
            Console.WriteLine("=============");
            if (demoCount == 0)
                Console.WriteLine("Parsing all demos");
            else
                Console.WriteLine($"Parsing {demoCount} demos");

            Console.WriteLine();
            var analyzer = new CsgoAnalyzer(@"D:\csgo Demos", demoCount);
            analyzer.Analyze();
        }
    }
}
