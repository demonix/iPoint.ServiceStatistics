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
            IObservable<long> bufferOpenings;
            Func<long, IObservable<long>> bufferClosingSelector;
            MovingWindowSequenceGenerator.Generate(1000*30, 1000*5*60, out bufferOpenings, out bufferClosingSelector);
            CountersDatabase db = CountersDatabase.Connect("127.0.0.1", null, "counters");
            IObservable<IList<EventPattern<LogEventArgs>>> observableEvents = receiver.ObservableEvents.Buffer(bufferOpenings, bufferClosingSelector);
            List<CounterAggregator> aggregators = new List<CounterAggregator>();
            Settings settings = new Settings();
            foreach (AggregationParameters parameters in settings.AggregationParameters)
            {
                CounterAggregator counterAggregator = new CounterAggregator(parameters, db);
                counterAggregator.BeginAggregation(observableEvents);
                aggregators.Add(counterAggregator);
            }
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
