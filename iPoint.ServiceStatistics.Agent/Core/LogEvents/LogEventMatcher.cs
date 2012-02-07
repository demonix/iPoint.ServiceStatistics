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

        public IEnumerable<LogEvent> FindMatches(string logFileName, string line)
        {
            foreach (LogEventDescription eventDescription in _eventDescriptions)
            {
                Match m = eventDescription.Rule.Match(line);
                if (!m.Success) continue;
                LogEvent evt = new LogEvent
                                   {
                                       Source = eventDescription.GetSource(logFileName, m),
                                       Category = eventDescription.GetCategory(logFileName, m),
                                       Counter = eventDescription.GetCounter(logFileName, m),
                                       Instance = eventDescription.GetInstance(logFileName, m),
                                       Type = eventDescription.EventType,
                                       DateTime =
                                           System.DateTime.ParseExact(eventDescription.GetDateTime(logFileName, m),
                                                                      eventDescription.DateFormat,
                                                                      CultureInfo.InvariantCulture),
                                       ExtendedData = eventDescription.GetExtendedData(logFileName, m),
                                       Value = eventDescription.GetValue(logFileName, m)
                                     
                                };
                yield return evt;
            }
            
        }
    }
}