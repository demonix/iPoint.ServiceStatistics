using System;
using System.Reactive.Linq;

namespace iPoint.ServiceStatistics.Server
{
    internal static class MovingWindowSequenceGenerator
    {
        public static void Generate(int moveEvery, int windowLength, out IObservable<long> openWindowSequence, out Func<long, IObservable<long>> closingWindowSequenceSelector)
        {
            openWindowSequence = Observable.Generate((long)1, x => true, x => x + 1, x => x, x => TimeSpan.FromMilliseconds(x *moveEvery <= windowLength ? 0 : moveEvery));
            closingWindowSequenceSelector = delegate (long i)
                                                {
                                                    long actualLength = i*moveEvery <= windowLength ? i*moveEvery : windowLength;
                                                    return Observable.Timer(TimeSpan.FromMilliseconds(actualLength));
                                                };

        }
    }
}