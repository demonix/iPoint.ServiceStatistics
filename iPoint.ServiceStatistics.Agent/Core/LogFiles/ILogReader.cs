using System;

namespace iPoint.ServiceStatistics.Agent.Core.LogFiles
{
    public interface ILogReader
    {
        event EventHandler<LineReadedEventArgs> LineReaded;
        event EventHandler<EventArgs> FinishedReading;
        void BeginRead();
        void Close();
    }
}