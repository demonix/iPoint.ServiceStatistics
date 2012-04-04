using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;

namespace iPoint.ServiceStatistics.Server
{
    public class CountersAutoDiscoverer
    {
        private readonly IObservable<LogEventArgs> _observableEvents;
        private readonly Settings _settings;
        private HashSet<string> _knownCounters = new HashSet<string>();

        public CountersAutoDiscoverer(IObservable<LogEventArgs> observableEvents, Settings settings)
        {
            _observableEvents = observableEvents;
            _settings = settings;
            
            int cnt = _settings.Aggregators.Count;
            for (int i = 0; i < cnt; i++)
            {
                CounterAggregator counterAggregator = _settings.Aggregators[i];
                if (!_knownCounters.Contains(counterAggregator.CounterCategory + "\t" + counterAggregator.CounterName))
                    _knownCounters.Add(counterAggregator.CounterCategory + "\t" + counterAggregator.CounterName);
            }
            

            
        }
        public void StartDiscovery()
        {
            _observableEvents.Subscribe(DiscoverNewCounters);
        }

        private object _lock = new object();

        private void DiscoverNewCounters(LogEventArgs eventArgs)
        {
            if (_knownCounters.Contains(eventArgs.LogEvent.Category + "\t" + eventArgs.LogEvent.Counter))
                return;

            lock (_lock)
            {
                if (_knownCounters.Contains(eventArgs.LogEvent.Category + "\t" + eventArgs.LogEvent.Counter))
                    return;

                File.AppendAllText(@"settings\AutoDiscoveredCounters.list",
                                   "\r\n" + eventArgs.LogEvent.Category + "\t" + eventArgs.LogEvent.Counter);
                DiscoverBasedOnGenericCounters(eventArgs);
                _knownCounters.Add(eventArgs.LogEvent.Category + "\t" + eventArgs.LogEvent.Counter);
            }
        }

        private void DiscoverBasedOnGenericCounters(LogEventArgs eventArgs)
        {
            int cnt = _settings.Aggregators.Count;
            for (int i = 0; i < cnt; i++)
            {
                CounterAggregator counterAggregator = _settings.Aggregators[i];
                if ((eventArgs.LogEvent.Category == counterAggregator.CounterCategory) &&
                    (eventArgs.LogEvent.Counter.StartsWith(counterAggregator.CounterName)) &&
                    (eventArgs.LogEvent.Counter.Length > counterAggregator.CounterName.Length))
                {
                    CounterAggregator aggregator = new CounterAggregator(counterAggregator.CounterCategory,
                                                                         eventArgs.LogEvent.Counter,
                                                                         counterAggregator.AggregationType,
                                                                         counterAggregator.InputType,"");
                    _settings.AddAggregator(aggregator);
                    File.AppendAllText(@"settings\counters.list",
                                       "\r\n" + aggregator.CounterCategory + "\t" + aggregator.CounterName + "\t" +
                                       aggregator.AggregationType.ToString() + "\t" + aggregator.InputType.FullName);
                }
            }
        }
    }
}