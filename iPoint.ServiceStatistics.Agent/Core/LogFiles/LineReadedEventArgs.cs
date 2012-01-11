using System;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;

namespace iPoint.ServiceStatistics.Agent.Core.LogFiles
{
    public class LineReadedEventArgs : EventArgs
    {
        public string Line { get; private set; }
        public LogEventMatcher LogEventMatcher { get; private set; }

        public LineReadedEventArgs(string line, LogEventMatcher logEventMatcher)
        {
            Line = line;
            LogEventMatcher = logEventMatcher;
        }
    }
}