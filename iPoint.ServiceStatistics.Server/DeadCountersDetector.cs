using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;

namespace iPoint.ServiceStatistics.Server
{
    public class DeadCountersDetector
    {
        private readonly ConcurrentDictionary<string, DateTime> _counterFreshnessTime =
            new ConcurrentDictionary<string, DateTime>();

        private IObservable<LogEventArgs> _observableEvents;
        private Settings _settings;

        public DeadCountersDetector(IObservable<LogEventArgs> observableEvents, Settings settings)
        {
            _observableEvents = observableEvents;
            _settings = settings;
            _observableEvents.Subscribe(UpdateCounterFreshnessTime);

        }

        private void UpdateCounterFreshnessTime(LogEventArgs eventArgs)
        {
            if (!_settings.Aggregators.Any(a => a.CounterCategory == eventArgs.LogEvent.Category && a.CounterName ==  eventArgs.LogEvent.Counter))
                return;

            string counter = String.Format("{0}_{1}_{2}_{3}", eventArgs.LogEvent.Category, eventArgs.LogEvent.Counter,
                                           eventArgs.LogEvent.Source, eventArgs.LogEvent.Instance);
            _counterFreshnessTime.AddOrUpdate(counter, DateTime.Now, (key, existingValue) => DateTime.Now);
        }

        public string GetCounterFreshnessTimeStats()
        {
            List<string> counters = _counterFreshnessTime.Keys.OrderBy(e => e).ToList();
            StringBuilder resultBuilder = new StringBuilder(counters.Count*255);
            DateTime dtNow = DateTime.Now;
            resultBuilder.AppendLine("Alivness stats for last 10 minutes");
            foreach (string counter in counters)
            {
                DateTime counterLastUpdateTime;
                if (_counterFreshnessTime.TryGetValue(counter, out counterLastUpdateTime))
                {
                    resultBuilder.AppendFormat("{0}\t({1})\t{2}\r\n",
                                               (dtNow - counterLastUpdateTime) > TimeSpan.FromMinutes(10) ? "0" : "1",
                                               counterLastUpdateTime,
                                               counter);
                }
            }
            return resultBuilder.ToString();

        }

    }
}