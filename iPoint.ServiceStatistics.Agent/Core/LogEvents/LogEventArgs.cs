using System;

namespace iPoint.ServiceStatistics.Agent.Core.LogEvents
{
    public class LogEventArgs : EventArgs
    {
        public LogEvent LogEvent{ get; private set; }

        public LogEventArgs(LogEvent logEvent)
        {
            LogEvent = logEvent;
        }
    }
}