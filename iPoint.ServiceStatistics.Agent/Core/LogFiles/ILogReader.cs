using System;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;

namespace iPoint.ServiceStatistics.Agent.Core.LogFiles
{
    public interface ILogReader: IDisposable
    {
        event EventHandler<LineReadedEventArgs> LineReaded;
        //event EventHandler<LogEventArgs> OnLogEvent;
        event EventHandler<EventArgs> FinishedReading;
        void BeginRead();
        void Close();
    }
}