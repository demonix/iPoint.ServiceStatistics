using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;
using iPoint.ServiceStatistics.Server.DataLayer;
using MyLib.Networking;

namespace iPoint.ServiceStatistics.Server
{
    
    class PerfEvent
    {
        public DateTime Timestamp { get; private set; }
        public double Value { get; private set; }
        public PerfEvent(double value)
        {
            Value = value;
            Timestamp = DateTime.Now;
        }
    }

    class Program
    {

        public static event EventHandler<LogEventArgs> OnLogEvent;

        static private void InvokeOnLogEvent(LogEventArgs e)
        {
            
            EventHandler<LogEventArgs> handler = OnLogEvent;
            if (handler != null) handler(null, e);
        }

        static void Main(string[] args)
        {
            

            //CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator = ".";
            AsyncTcpServer srv = new AsyncTcpServer(new IPEndPoint(IPAddress.Any, 50001));
            srv.Start();
            //srv.MessageReceived += srv_MessageReceived;
            MessageReceiver receiver = new MessageReceiver(srv);
            //IObservable<long> bufferOpenings = null;
            //Func<long, IObservable<long>> bufferClosingSelector = null;
            MovingWindowSequence seq = new MovingWindowSequence(1000*60, 1000*5*60);
            //MovingWindowSequence.Generate(, ref bufferOpenings, ref bufferClosingSelector);
            //CountersDatabase db = CountersDatabase.Connect("127.0.0.1", null, "counters");
            CountersDatabase.InitConnection("127.0.0.1", null, "counters");
            
            var observableEvents = receiver.ObservableEvents.Buffer(seq.BufferOpenings, seq.ClosingWindowSequenceSelector).Publish();
            observableEvents.Connect();

            //var observableEvents = Observable.Empty<LogEventArgs>().Buffer(seq.BufferOpenings, seq.ClosingWindowSequenceSelector).Publish();
            
            Settings settings = new Settings();
            settings.ObservableEvents = observableEvents;
            settings.ReadAggregators();
 
            CountersAutoDiscoverer countersAutoDiscoverer = new CountersAutoDiscoverer(receiver.ObservableEvents, settings);
            countersAutoDiscoverer.StartDiscovery();

            observableEvents.Subscribe(l => Console.WriteLine("Total events: " + l.Count));
            
            ConsoleKeyInfo keyInfo;
            while ((keyInfo = Console.ReadKey()).Key!=ConsoleKey.Enter)
            {
                if (keyInfo.Key == ConsoleKey.Add)
                {
                    seq.IncreaseInterval();
                    Console.WriteLine("Interval is {0} ms now", seq.MoveEvery);
                }
                if (keyInfo.Key == ConsoleKey.Subtract)
                {
                    seq.DecreaseInterval();
                    Console.WriteLine("Interval is {0} ms now", seq.MoveEvery);
                }
                if (keyInfo.Key == ConsoleKey.R)
                {
                    settings.ReadAggregators();
                    Console.WriteLine("Aggregators were updated");
                }
            }
        }

        private static EventPattern<LogEventArgs> Transform(EventPattern<LogEventArgs> eventPattern)
        {
            LogEvent logEvent = eventPattern.EventArgs.LogEvent;
            string counterNameReplacement = Settings.ExtendedDataTransformations.GetCounterNameReplacement(logEvent.ExtendedData);
            /* if (!String.IsNullOrEmpty(counterNameReplacement))
                 Console.WriteLine(logEvent.ExtendedData +" --> " + counterNameReplacement);
            */
            logEvent.Counter += counterNameReplacement;
            return eventPattern;
        }


        /* static void srv_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream(e.Message.MessageData);
            LogEvent le = (LogEvent) bf.Deserialize(ms);
            InvokeOnLogEvent(new LogEventArgs(le));
        }
       */
      

        private static int i;
        static object _locker = new object();
        
        
    }
}
