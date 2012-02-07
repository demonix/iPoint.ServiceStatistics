using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace iPoint.ServiceStatistics.Server
{
    public class MovingWindowSequence
    {
        public MovingWindowSequence(int moveEvery, int windowLength)
        {
            MoveEvery = moveEvery;
            WindowLength = windowLength;
            BufferOpenings = Observable.Generate((long)1, x => true, x => x + 1, x => x, x => TimeSpan.FromMilliseconds(x * MoveEvery <= WindowLength ? 0 : MoveEvery));
            ClosingWindowSequenceSelector = delegate(long i)
            {
                long actualLength = i * MoveEvery <= WindowLength ? i * MoveEvery : WindowLength;
                return Observable.Timer(TimeSpan.FromMilliseconds(actualLength));
            };
        }

        public IObservable<long> BufferOpenings;
        public Func<long, IObservable<long>> ClosingWindowSequenceSelector;
        public int MoveEvery { get; private set; }
        public int WindowLength { get; private set; }


        public void IncreaseInterval()
        {
            MoveEvery += 1000;
        }

        public void DecreaseInterval()
        {
            MoveEvery -= 1000;
        }
    }
}