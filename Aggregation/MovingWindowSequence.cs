using System;
using System.Reactive.Linq;
using System.Threading;

namespace Aggregation
{
    public class MovingWindowSequence:IDisposable
    {
       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="moveEvery">Window move step time in milliseconds</param>
        /// <param name="windowLength">Window length in milliseconds</param>
        public MovingWindowSequence(int moveEvery, int windowLength)
        {
            MoveEvery = moveEvery;
            WindowLength = windowLength;
            BufferOpenings = Observable.Generate((long) 1, x => true, x => Interlocked.Increment(ref x), x => x,
                                                 x =>
                                                     {
                                                         //Console.WriteLine("Open "+x);
                                                         return
                                                             TimeSpan.FromMilliseconds(x*MoveEvery <= WindowLength
                                                                                           ? 0
                                                                                           : MoveEvery);
                                                     });
            ClosingWindowSequenceSelector = i =>
                                                {
                                                    long actualLength = i*MoveEvery <= WindowLength
                                                                            ? i*MoveEvery
                                                                            : WindowLength;
                                                    var res = Observable.Return(i).Delay(TimeSpan.FromMilliseconds(actualLength));
                                                    //res.Subscribe(x=> Console.WriteLine("Closed "+x));
                                                    return res;
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

        public int WindowLength { get; private set; }


        public void IncreaseInterval(int miliseconds)
        {
            Interlocked.Add(ref _moveEvery, miliseconds);
        }

        public void DecreaseInterval(int miliseconds)
        {
            Interlocked.Add(ref _moveEvery, -miliseconds);
        }

        public void Dispose()
        {
            
        }
    }
}