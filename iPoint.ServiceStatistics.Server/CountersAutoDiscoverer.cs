using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;

namespace iPoint.ServiceStatistics.Server
{
    public class CountersAutoDiscoverer
    {
        private readonly IObservable<EventPattern<LogEventArgs>> _observableEvents;
        private readonly IObservable<IList<EventPattern<LogEventArgs>>> _eventsBuffer;
        private readonly List<CounterAggregator> _aggregators;
        private readonly Settings _settings;
        private HashSet<string> _addedCounters = new HashSet<string>();

        public CountersAutoDiscoverer(IObservable<EventPattern<LogEventArgs>> observableEvents, IObservable<IList<EventPattern<LogEventArgs>>> eventsBuffer, List<CounterAggregator> aggregators, Settings settings)
        {
            _aggregators = aggregators;
            foreach (CounterAggregator counterAggregator in _aggregators)
            {
                if (!_addedCounters.Contains(counterAggregator.CounterCategory + "\t" + counterAggregator.CounterName))
                    _addedCounters.Add(counterAggregator.CounterCategory + "\t" + counterAggregator.CounterName);
            }
            _observableEvents = observableEvents;
            _eventsBuffer = eventsBuffer;
            _observableEvents.Subscribe(DiscoverNewCounters);
            _settings = settings;
        }

        private object _lock = new object();

        private void DiscoverNewCounters(EventPattern<LogEventArgs> eventPattern)
        {
            if (_addedCounters.Contains(eventPattern.EventArgs.LogEvent.Category + "\t" +eventPattern.EventArgs.LogEvent.Counter))
                return;

            int cnt = _aggregators.Count;

            for (int i = 0; i < cnt; i++)
            {
                CounterAggregator counterAggregator = _aggregators[i];
                if ((eventPattern.EventArgs.LogEvent.Category == counterAggregator.CounterCategory)&&
                    (eventPattern.EventArgs.LogEvent.Counter.StartsWith(counterAggregator.CounterName))&&
                    (eventPattern.EventArgs.LogEvent.Counter.Length > counterAggregator.CounterName.Length))
                {
                    lock (_addedCounters)
                    {
                        if (_addedCounters.Contains(eventPattern.EventArgs.LogEvent.Category + "\t" + eventPattern.EventArgs.LogEvent.Counter))
                            return;

                        CounterAggregator aggregator = new CounterAggregator(counterAggregator.CounterCategory,
                                                                             eventPattern.EventArgs.LogEvent.Counter,
                                                                             counterAggregator.AggregationType,
                                                                             counterAggregator.InputType);
                        aggregator.BeginAggregation(_eventsBuffer);
                        _aggregators.Add(aggregator);
                        _addedCounters.Add(eventPattern.EventArgs.LogEvent.Category + "\t" +
                                           eventPattern.EventArgs.LogEvent.Counter);
                        File.AppendAllText(@"settings\counters.list",
                                           "\r\n" + aggregator.CounterCategory + "\t" + aggregator.CounterName + "\t" +
                                           aggregator.AggregationType.ToString() + "\t" + aggregator.InputType.FullName);

                    }
                    return;
                }
            }
        }
    }
}