using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization.Formatters.Binary;
using EventEvaluationLib;
using MyLib.Networking;

namespace iPoint.ServiceStatistics.Server
{
    public class MessageReceiver:IDisposable
    {
        private AsyncTcpServer  _asyncTcpServer;
        private Subject<LogEventArgs> _eventSubject;
        public IObservable<LogEventArgs> ObservableEvents { get { return _eventSubject.AsObservable(); }}
   
        
        public MessageReceiver(AsyncTcpServer asyncTcpServer)
        {
            _asyncTcpServer = asyncTcpServer;
            _asyncTcpServer.MessageReceived += srv_MessageReceived;
            _eventSubject = new Subject<LogEventArgs>();
        }

        private void InvokeOnLogEvent(LogEventArgs e)
        {
            string counterNameReplacement = Settings.ExtendedDataTransformations.GetCounterNameReplacement(e.LogEvent.ExtendedData);
            e.LogEvent.Counter += counterNameReplacement;
            _eventSubject.OnNext(e);
        }

        void srv_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            LogEvent le = LogEvent.Deserialize(e.Message.MessageData);
            InvokeOnLogEvent(new LogEventArgs(le));
        }

        public void Dispose()
        {
            _asyncTcpServer.MessageReceived -= srv_MessageReceived;
        }
    }
}