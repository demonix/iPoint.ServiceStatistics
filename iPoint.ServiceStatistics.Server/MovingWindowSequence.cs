using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace iPoint.ServiceStatistics.Server
{
    public class MovingWindowSequence
    {
       

        public MovingWindowSequence(int moveEvery, int windowLength)
        {

            MoveEvery = moveEvery;
            WindowLength = windowLength;
            BufferOpenings = Observable.Generate((long)1, x => true, x => Interlocked.Increment(ref x), x =>
                                                                                     {
                                                                                         Console.WriteLine(x );
                                                                                         return x;
                                                                                     }
                                                                                     ,
                                                                                     x => TimeSpan.FromMilliseconds(x * MoveEvery <= WindowLength ? 0 : MoveEvery));
            ClosingWindowSequenceSelector = delegate(long i)
            {
                
                long actualLength = i * MoveEvery <= WindowLength ? i * MoveEvery : WindowLength;
                Console.WriteLine("begin new window with length of "+ actualLength+ " ms.");
                return Observable.Timer(TimeSpan.FromMilliseconds(actualLength));
            };
        }

        public IObservable<long> BufferOpenings;
        public Func<long, IObservable<long>> ClosingWindowSequenceSelector;
        private int _moveEvery;
        public int MoveEvery
        {
            get { return _moveEvery; }
            private set { _moveEvery = value; }
        }

        private int _windowLength;
        public int WindowLength
        {
            get { return _windowLength; }
            private set { _windowLength = value; }
        }


        public void IncreaseInterval()
        {
            Interlocked.Add(ref _moveEvery, 1000);
        }

        public void DecreaseInterval()
        {
            Interlocked.Add(ref _moveEvery, -1000);
        }
    }
}