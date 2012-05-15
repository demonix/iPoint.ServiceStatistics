using System;

namespace EventEvaluationLib.LogReaders
{
    public interface ILogReader: IDisposable
    {
        event EventHandler<LineReadedEventArgs> LineReaded;
        event EventHandler<EventArgs> FinishedReading;
        void BeginRead();
        void Close();
    }
}