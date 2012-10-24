using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Aggregation.Experimental.AggregationOperations;
using EventEvaluationLib;


namespace Aggregation.Experimental
{
    using System.Collections.Concurrent;

    public class Aggregator:IDisposable
    {
        private readonly MovingWindowSequence _seq;
        BlockingCollection<LogEvent> _eventBuffer = new BlockingCollection<LogEvent>();
        private readonly Subject<LogEvent> _eventSubject = new Subject<LogEvent>();
        private readonly IConnectableObservable<IObservable<LogEvent>> _logEventBuffer;
        private readonly Subject<IList<AggregatedValue>> _aggregationCompleteSubject = new Subject<IList<AggregatedValue>>();
        private readonly string _counterCategoryFilter;
        private readonly string _counterNameFilter;
        private readonly Type _valuesDataType;
        public IObservable<IList<AggregatedValue>> AggregationComplete { get { return _aggregationCompleteSubject.AsObservable(); } }
        private readonly IAggregationOperation _aggregationOperation;
        private volatile bool _started;
        private IDisposable _unsubscriptionToken;

        public Aggregator(string counterCategoryFilter, string counterNameFilter, string aggregationType, string aggregationParameters, Type valuesDataType)
        {
            _aggregationOperation = AggregationOperationFactory.Create(valuesDataType, aggregationType, aggregationParameters);
            _counterCategoryFilter = counterCategoryFilter;
            _counterNameFilter = counterNameFilter;
            _valuesDataType = valuesDataType;
            _seq = new MovingWindowSequence(1000, 1000*5);
            _logEventBuffer = _eventSubject.Window(_seq.BufferOpenings, _seq.ClosingWindowSequenceSelector).Publish();
            _logEventBuffer.Connect();

        }

        public Aggregator(AggregatorSettings aggregatorSettings)
            : this(aggregatorSettings.CounterCategoryFilter, aggregatorSettings.CounterNameFilter, aggregatorSettings.AggregationType, aggregatorSettings.AggregationParameters,aggregatorSettings.ValuesDataType)
        {
            
        }

        public bool CanProcess(LogEvent logEvent)
        {
            return _started && logEvent.Category == _counterCategoryFilter && logEvent.Counter == _counterNameFilter;
        }
        
       

        private void Aggregate(IObservable<LogEvent> events)
        {
            Guid g = Guid.NewGuid();
            Console.WriteLine(g + " opened at "+ DateTime.Now);
            List<LogEvent> allEvents = new List<LogEvent>();
            events.ObserveOn(TaskPoolScheduler.Default).Subscribe(
                evt => allEvents.Add(evt),
                () =>
            {
                Console.WriteLine(g + " closed at " + DateTime.Now);
                var gr = GroupCounters(allEvents);
                _aggregationCompleteSubject.OnNext(gr.Select(gouppedValues =>
                {
                    AggregatedValue result =
                        new AggregatedValue(
                            gouppedValues.Key,
                            _aggregationOperation.Do(
                                gouppedValues.ToList()));
                    Console.WriteLine(DateTime.Now + " Aggregated on thread #" + Thread.CurrentThread.ManagedThreadId);
                    return result;
                }).ToList());
                Console.WriteLine(g + " Finished on thread #" + Thread.CurrentThread.ManagedThreadId+"\r\n"+string.Join("\r\n",allEvents));
            });
        }

        public void Push(LogEvent logEvent)
        {
            _eventSubject.OnNext(logEvent);
        }

        private IEnumerable<IGrouping<CounterGroup, UniversalValue>> GroupCounters(IEnumerable<LogEvent> input)
        {
            var groups = new List<CounterGroup.GroupBy>()
                             {
                                 CounterGroup.GroupBy.NoGrpopping,
                                 CounterGroup.GroupBy.Source,
                                 CounterGroup.GroupBy.Instance,
                                 CounterGroup.GroupBy.ExtendedData,
                                 CounterGroup.GroupBy.Source | CounterGroup.GroupBy.Instance,
                                 CounterGroup.GroupBy.Source | CounterGroup.GroupBy.ExtendedData,
                                 CounterGroup.GroupBy.Instance | CounterGroup.GroupBy.ExtendedData,
                                 CounterGroup.GroupBy.Source | CounterGroup.GroupBy.Instance | CounterGroup.GroupBy.ExtendedData
                             };

            IEnumerable<IGrouping<CounterGroup, UniversalValue>> result = groups.SelectMany(group => input.GroupBy(logEvent => CreateCounterGroup(group, logEvent), logEvent => UniversalValue.ParseFromString(_valuesDataType, logEvent.Value)));
            return result;

        }

        private CounterGroup CreateCounterGroup(CounterGroup.GroupBy groupBy, LogEvent eventArgs)
        {
            return new CounterGroup(
                eventArgs.Counter,
                groupBy.HasFlag(CounterGroup.GroupBy.Source)? "ALL_SOURCES" : eventArgs.Source,
                groupBy.HasFlag(CounterGroup.GroupBy.Instance) ? "ALL_INSTANCES" : eventArgs.Instance,
                groupBy.HasFlag(CounterGroup.GroupBy.ExtendedData) ? "ALL_EXTDATA" : eventArgs.ExtendedData
                );
        }

        public void Start()
        {
            _unsubscriptionToken = _logEventBuffer.Subscribe(Aggregate);
            _started = true;
        }

        public void Stop()
        {
            _started = false;
            _unsubscriptionToken.Dispose();
        }

        public void Dispose()
        {
            if (_started)
                Stop();
            _aggregationCompleteSubject.Dispose();
            _eventSubject.Dispose();
           
        }
    }
}