using System;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;

namespace iPoint.ServiceStatistics.Agent.Core.LogFiles
{
    public class LineReadedEventArgs : EventArgs
    {
        public string LogFileName { get; private set; }
        public string Line { get; private set; }
        //public LogEventEvaluator LogEventEvaluator { get; private set; }

        public LineReadedEventArgs(string logFileName, string line/*, LogEventEvaluator logEventEvaluator*/)
        {
            LogFileName = logFileName;
            Line = line;
            //LogEventEvaluator = logEventEvaluator;
        }
    }
}