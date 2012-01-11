using System;

namespace iPoint.ServiceStatistics.Agent
{
    internal class LogWatcherEventArgs: EventArgs
    {
        public string FullPath { get; private set; }
        public LogWatcherEventArgs(string fullPath)
        {
            FullPath = fullPath;
        }
    }
}