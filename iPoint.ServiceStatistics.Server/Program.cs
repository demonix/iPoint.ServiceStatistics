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
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;
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
            AsyncTcpServer srv = new AsyncTcpServer(new IPEndPoint(IPAddress.Any, 50001));
            srv.Start();
            //srv.MessageReceived += srv_MessageReceived;
            MessageReceiver receiver = new MessageReceiver(srv);
            IObservable<long> oneSecondBufferOpenings;
            Func<long, IObservable<long>> fiveSecondsBufferClosingSelector;
            MovingWindowSequenceGenerator.Generate(1000, 1000*5, out oneSecondBufferOpenings, out fiveSecondsBufferClosingSelector);

            CounterAggregatorSettings counterAggregatorSettings = new CounterAggregatorSettings("PrintServer", null, "IncomingRequestCount", CounterAggregationType.Sum, typeof(Int32));
            //CounterAggregatorSettings counterAggregatorSettings = new CounterAggregatorSettings("FT", null, "RN_Sent", CounterAggregationType.Sum, typeof(Int32));
            counterAggregatorSettings.UnsubscriptionToken = receiver.ObservableEvents.Buffer(oneSecondBufferOpenings, fiveSecondsBufferClosingSelector)
                .Select(counterAggregatorSettings.EventSelector)
                .Subscribe(counterAggregatorSettings.AggregationAction);
            Console.ReadKey();
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
