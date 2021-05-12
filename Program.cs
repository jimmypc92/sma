using System;
using System.Threading.Tasks;

namespace sma
{
    class Program
    {
        static async Task Main(string[] args)
        {
            TimeSpan smaDuration = TimeSpan.FromSeconds(3);

            SMA sma = new SMA(11, smaDuration);

            Random rand = new Random();

            DateTimeOffset lastWait = DateTimeOffset.UtcNow;

            while (true)
            {
                int addend = rand.Next(100) + 1;

                Console.WriteLine($"Adding {addend}");

                sma.IncrementCurrentBucket(addend);

                Console.Write("Buckets: ");

                foreach (int bucket in sma._tracker.Buckets)
                {
                    Console.Write($"{bucket} ");
                }

                Console.WriteLine();

                Console.WriteLine($"SMA: {sma.GetLatestAverage()}");

                await Task.Delay(500);

                if (DateTimeOffset.UtcNow - lastWait > 5 * smaDuration)
                {
                    lastWait = DateTimeOffset.UtcNow;

                    TimeSpan delay = 2 * smaDuration + TimeSpan.FromMilliseconds(100);

                    Console.WriteLine($"Waiting {delay.TotalMilliseconds} ms");

                    //
                    // Demonstrate dead intervals
                    await Task.Delay(delay);
                }
            }
        }
    }
}
