using System;

namespace EventEvaluationLib
{
    public class LogEventArgs : EventArgs
    {
        public LogEvent LogEvent { get; private set; }
        public LogEventArgs(LogEvent logEvent)
        {
            LogEvent = logEvent;
        }
    }
}