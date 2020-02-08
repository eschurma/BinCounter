using System;

namespace ExampleUsage {
    class Program {
        static void Main(string[] args) {
            BinCounter b = new BinCounter(30, 0, 2);

            // Log a bunch of distributed data
            Random r = new Random();
            for(int i = 0; i < 10000; i++) {
                float dir = r.NextDouble() < .5 ? -1 : 1;
                float dist = (float)Math.Pow(r.NextDouble(), 2);
                b.Log(1 + dir * dist);
            }

            // Log a few outliers
            b.Log(5f);
            b.Log(-3f);

            Console.WriteLine("TotalEntries: " + b.TotalObservations);
            Console.WriteLine("Mean: " + b.Mean);
            Console.WriteLine("A particular bin count: " + b.Bins[4]);
            Console.WriteLine("\nFull histogram plus info:\n------" + b.GetHistogram());
        }
    }
}