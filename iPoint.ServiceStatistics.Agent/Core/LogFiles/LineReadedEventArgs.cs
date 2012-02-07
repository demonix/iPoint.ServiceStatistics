using System;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;

namespace iPoint.ServiceStatistics.Agent.Core.LogFiles
{
    public class LineReadedEventArgs : EventArgs
    {
        public string LogFileName { get; private set; }
        public string Line { get; private set; }
        public LogEventMatcher LogEventMatcher { get; private set; }

        public LineReadedEventArgs(string logFileName, string line, LogEventMatcher logEventMatcher)
        {
            LogFileName = logFileName;
            Line = line;
            LogEventMatcher = logEventMatcher;
        }
    }
}