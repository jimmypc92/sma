using System;
using System.Threading;

public class SMA
{
    private readonly TimeSpan _calculationInterval;

    //
    // Should be private for real usage, but internal for demo
    internal Tracker _tracker;

    internal class Tracker
    {
        public int Index { get; set; }

        public DateTimeOffset LastUpdateTime { get; set; }

        public int[] Buckets { get; set; }

        public long Sum { get; set; }

        public int CurrentBucketValue { get; set; }
    }

    public SMA(int bucketCount, TimeSpan calculationInterval)
    {
        if (bucketCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bucketCount));
        }

        if (calculationInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(calculationInterval));
        }

        _calculationInterval = calculationInterval;

        _tracker = new Tracker
        {
            Index = 0,
            Sum = 0,
            CurrentBucketValue = 0,
            LastUpdateTime = DateTimeOffset.UtcNow,
            Buckets = new int[bucketCount]
        };
    }

    public void IncrementCurrentBucket(int count)
    {
        DateTimeOffset updateTime = DateTimeOffset.UtcNow;

        Tracker oldTracker;
        Tracker newTracker;

        do
        {
            oldTracker = _tracker;

            TimeSpan elapsed = updateTime - _tracker.LastUpdateTime;

            if (elapsed < _calculationInterval)
            {
                newTracker = new Tracker
                {
                    Sum = oldTracker.Sum,
                    Buckets = oldTracker.Buckets,
                    Index = oldTracker.Index,
                    LastUpdateTime = oldTracker.LastUpdateTime,
                    CurrentBucketValue = oldTracker.CurrentBucketValue + count
                };
            }
            else
            {
                //
                // On this code path we have to allocate a new array.
                // Any in-place updates on the existing buckets can cause concurrent access problems
                int[] newBuckets = new int[oldTracker.Buckets.Length];

                Array.Copy(oldTracker.Buckets, newBuckets, newBuckets.Length);

                int index = oldTracker.Index;

                int elapsedIntervals = (int)(elapsed / _calculationInterval);

                //
                // Set latest promoted bucket value
                newBuckets[index] = oldTracker.CurrentBucketValue;

                index = (index + 1) % newBuckets.Length;

                //
                // backfill 0's for unused intervals
                for (int i = 1; i < elapsedIntervals; i++)
                {
                    newBuckets[index] = 0;

                    index = (index + 1) % newBuckets.Length;
                }

                //
                // Calculate new sum
                int sum = 0;

                foreach (int bucket in newBuckets)
                {
                    sum += bucket;
                }

                newTracker = new Tracker
                {
                    Sum = sum,
                    Buckets = newBuckets,
                    Index = index,
                    LastUpdateTime = updateTime,
                    CurrentBucketValue = count
                };
            }
        }
        while (Interlocked.CompareExchange(ref _tracker, newTracker, oldTracker) != oldTracker);
    }

    public double GetLatestAverage()
    {
        //
        // Force fresh if necessary
        if (DateTimeOffset.UtcNow - _tracker.LastUpdateTime > _calculationInterval)
        {
            IncrementCurrentBucket(0);
        }

        //
        // Tracker is replaced not updated in place, Interlocked read unnecessary even on 32-bit system
        return (double)((double)_tracker.Sum / _tracker.Buckets.Length);
    }
}
