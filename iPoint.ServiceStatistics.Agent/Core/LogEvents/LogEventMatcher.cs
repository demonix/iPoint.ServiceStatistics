using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using iPoint.ServiceStatistics.Agent.Core.LogFiles;

namespace iPoint.ServiceStatistics.Agent.Core.LogEvents
{
    public class LogEventMatcher
    {
        private List<LogEventDescription> _eventDescriptions;

        public LogEventMatcher(List<LogEventDescription> eventDescriptions)
        {
            _eventDescriptions = eventDescriptions;
        }

        public IEnumerable<LogEvent> FindMatches(string line)
        {
            foreach (LogEventDescription eventDescription in _eventDescriptions)
            {
                Match m = eventDescription.Rule.Match(line);
                if (!m.Success) continue;
                LogEvent evt = new LogEvent
                                   {
                                       Source = eventDescription.GetSource(m),
                                       Category = eventDescription.GetCategory(m),
                                       Counter = eventDescription.GetCounter(m),
                                       Instance = eventDescription.GetInstance(m),
                                       Type = eventDescription.EventType,
                                       DateTime =
                                           System.DateTime.ParseExact(eventDescription.GetDateTime(m),
                                                                      eventDescription.DateFormat,
                                                                      CultureInfo.InvariantCulture),
                                       Data = new EventData(null, eventDescription.GetValue(m))
                                     
                                };
                yield return evt;
            }
            
        }
    }
}