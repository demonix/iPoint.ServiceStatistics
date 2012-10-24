using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Aggregation.Experimental.RxExtensions
{
    public static class ConsumingExtensions
    {
        public static IObservable<TResult> Consume<TSource, TResult>(
            this IObservable<TSource> source,
            Func<TSource, Maybe<TResult>> consumeNext,
            IScheduler scheduler)
        {

            return source.Consume(
                value => Observable.Create<TResult>(
                    observer => scheduler.Schedule(() =>
                    {
                        Maybe<TResult> result;

                        do
                        {
                            try
                            {
                                result = consumeNext(value);
                            }
                            catch (Exception ex)
                            {
                                observer.OnError(ex);
                                return;
                            }

                            if (result.HasValue)
                            {
                                observer.OnNext(result.Value);
                            }
                        }
                        while (result.HasValue);

                        observer.OnCompleted();
                    })));
        }

        public static void SetDisposableIndirectly(this SerialDisposable disposable, Func<IDisposable> factory)
        {

            var indirection = new SingleAssignmentDisposable();

            disposable.Disposable = indirection;

            indirection.Disposable = factory();
        }

        public static IObservable<TResult> Consume<TSource, TResult>(
            this IObservable<TSource> source, Func<TSource, IObservable<TResult>> consumerSelector)
        {

            return Observable.Create<TResult>(
                observer =>
                {
                    object gate = new object();
                    var consumingSubscription = new SerialDisposable();
                    var schedule = new SerialDisposable();

                    TSource lastSkippedNotification = default(TSource);
                    bool hasSkippedNotification = false;
                    bool consuming = false;
                    bool stopped = false;

                    var subscription = source.Subscribe(
                        value =>
                        {
                            lock (gate)
                            {
                                if (consuming)
                                {
                                    lastSkippedNotification = value;
                                    hasSkippedNotification = true;
                                    return;
                                }
                                else
                                {
                                    consuming = true;
                                    hasSkippedNotification = false;
                                }
                            }

                            var additionalData = value;

                            schedule.Disposable = Scheduler.Immediate.Schedule(
                                self =>
                                {
                                    IObservable<TResult> observable;

                                    try
                                    {
                                        observable = consumerSelector(additionalData);
                                    }
                                    catch (Exception ex)
                                    {
                                        observer.OnError(ex);
                                        return;
                                    }

                                    consumingSubscription.SetDisposableIndirectly(
                                        () => observable.Subscribe(
                                            observer.OnNext,
                                            observer.OnError,
                                            () =>
                                            {
                                                bool consumeAgain = false;
                                                bool completeNow = false;

                                                lock (gate)
                                                {
                                                    /* The hasSkippedNotification field avoids a race condition between source notifications and the consuming observable 
                                                    * calling OnCompleted that could cause data to become available without any active consumer.  The solution is to 
                                                    * check whether additional notifications were skipped before the consuming observable completed.  If so, then we 
                                                    * try to consume once more; and if there isn't any data available, because it was already consumed before the previous 
                                                    * observable completed, then the new observable will be empty.  If no additional notifications are received 
                                                    * from the source before the new observable completes, then hasSkippedNotification will be false the second time 
                                                    * around and there will be no active consumer until the next source notification.
                                                    * 
                                                    * This behavior reactively mimics typical interactive consumer behavior; e.g., lock and loop if queue.Count > 0.
                                                    */
                                                    consuming = hasSkippedNotification;

                                                    if (consuming)
                                                    {
                                                        additionalData = lastSkippedNotification;
                                                        hasSkippedNotification = false;
                                                        consumeAgain = true;
                                                    }
                                                    else
                                                    {
                                                        completeNow = stopped;


                                                    }


                                                }

                                                if (consumeAgain)
                                                {
                                                    self();
                                                }
                                                else if (completeNow)
                                                {
                                                    observer.OnCompleted();
                                                }
                                            }));
                                });
                        },
                        observer.OnError,
                        () =>
                        {
                            bool completeNow;

                            lock (gate)
                            {
                                stopped = true;

                                completeNow = !consuming;
                            }

                            if (completeNow)
                            {
                                observer.OnCompleted();
                            }
                        });

                    return new CompositeDisposable(consumingSubscription, subscription, schedule);
                });
        }
    }
}