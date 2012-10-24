using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using EventEvaluationLib;
using MyLib.Networking;
using System.Reactive.Subjects;

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


    public class MessageReceiverEx : IDisposable 
    {

        private AsyncTcpServer _asyncTcpServer;
        public IObservable<EventPattern<MessageReceivedEventArgs>> MessageReceived { get; private set; }
        public IObservable<string>  LogEvents { get; private set; }


        public MessageReceiverEx(AsyncTcpServer asyncTcpServer)
        {
            _asyncTcpServer = asyncTcpServer;
            MessageReceived = Observable.FromEventPattern<MessageReceivedEventArgs>(_asyncTcpServer, "MessageReceived");
            Random rnd = new Random();
            LogEvents =
                Observable.Create<string>(
                    o =>
                        {
                            return 
                            MessageReceived.Subscribe(
                                data =>
                                    {
                                        string value = Encoding.UTF8.GetString(data.EventArgs.Message.MessageData);
                                        Console.WriteLine("Received "+value + " at "+ Thread.CurrentThread.ManagedThreadId);
                                        o.OnNext(value);
                                    });
                       });
            
        }

        
        public void Dispose()
        {
            
        }
    }

    

}