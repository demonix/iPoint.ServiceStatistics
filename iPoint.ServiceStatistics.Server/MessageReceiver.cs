using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization.Formatters.Binary;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;
using MyLib.Networking;

namespace iPoint.ServiceStatistics.Server
{
    public class MessageReceiver:IDisposable
    {
        private AsyncTcpServer  _asyncTcpServer;
        //public event EventHandler<LogEventArgs> OnLogEvent;
        //public IObservable<EventPattern<LogEventArgs>> ObservableEvents;
        private Subject<LogEventArgs> _eventSubject;
        public IObservable<LogEventArgs> ObservableEvents { get { return _eventSubject.AsObservable(); }}
   
        
        public MessageReceiver(AsyncTcpServer asyncTcpServer)
        {
            _asyncTcpServer = asyncTcpServer;
            _asyncTcpServer.MessageReceived += srv_MessageReceived;
            //ObservableEvents = Observable.FromEventPattern<LogEventArgs>(this, "OnLogEvent");
            _eventSubject = new Subject<LogEventArgs>();
        }

        private void InvokeOnLogEvent(LogEventArgs e)
        {

            string counterNameReplacement = Settings.ExtendedDataTransformations.GetCounterNameReplacement(e.LogEvent.ExtendedData);
            e.LogEvent.Counter += counterNameReplacement;
            _eventSubject.OnNext(e);
           // if (OnLogEvent == null)
           //     return;


                //async
                /*
                Delegate[] delegates = OnLogEvent.GetInvocationList();
                foreach (EventHandler<LogEventArgs> handler in delegates)
                {
                    handler.BeginInvoke(null, e, EndInvokeOnLogEvent, handler);    
                }*/
                //sync
                //lock (_asyncTcpServer)
                //    File.AppendAllText("temp.log", DateTime.Now + " received " + e.LogEvent.Counter+"\r\n");

            //string counterNameReplacement = Settings.ExtendedDataTransformations.GetCounterNameReplacement(e.LogEvent.ExtendedData);
                //lock (_asyncTcpServer)
                //    File.AppendAllText("temp.log", DateTime.Now + " transform " + e.LogEvent.ExtendedData + " to " + counterNameReplacement + "\r\n");
            //e.LogEvent.Counter += counterNameReplacement;
                //lock(_asyncTcpServer)
                //    File.AppendAllText("temp.log", DateTime.Now + " is now " + e.LogEvent.Counter + "\r\n");
            //OnLogEvent(null, e);

        }

        private void EndInvokeOnLogEvent(IAsyncResult ar)
        {
            (ar.AsyncState as EventHandler<LogEventArgs>).EndInvoke(ar);
        }

        void srv_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream(e.Message.MessageData);
            LogEvent le = (LogEvent)bf.Deserialize(ms);
            InvokeOnLogEvent(new LogEventArgs(le));
        }

        public void Dispose()
        {
            _asyncTcpServer.MessageReceived -= srv_MessageReceived;
        }
    }
}