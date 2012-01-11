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

        public static event EventHandler<LogEventArgs> onLogEvent;

        static private void InvokeOnLogEvent(LogEventArgs e)
        {
            EventHandler<LogEventArgs> handler = onLogEvent;
            if (handler != null) handler(null, e);
        }

        protected delegate void OnNextDelegate(IList<EventPattern<LogEventArgs>> events);
        private static OnNextDelegate _onNextDelegate;

        static void Main(string[] args)
        {
            /*string perSecondLog = @"\\VM-WAC\d$\iPoint.Server\Storage\counters\57D43C86-1054-4C98-882F-F5A45B23FA11";
            string perFiveMinutesLog = @"\\VM-WAC\d$\iPoint.Server\Storage\counters\23466372-6BDA-42BE-8844-73AEF0DE0C07";
            
            if (!File.Exists(perSecondLog))
                File.Create(perSecondLog).Close();

            if (!File.Exists(perFiveMinutesLog))
                File.Create(perFiveMinutesLog).Close();

            FileStream fs = new FileStream(perSecondLog,FileMode.Open,FileAccess.Write,FileShare.Read);
            fs.Seek(0, SeekOrigin.End);
            StreamWriter writer = new StreamWriter(fs);

            FileStream fs2 = new FileStream(perFiveMinutesLog, FileMode.Open, FileAccess.Write, FileShare.Read);
            fs2.Seek(0, SeekOrigin.End);
            StreamWriter writer2 = new StreamWriter(fs2);
             */


            AsyncTcpServer srv = new AsyncTcpServer(new IPEndPoint(IPAddress.Any, 50001));
            srv.Start();
            srv.MessageReceived += srv_MessageReceived;
            MessageReceiver receiver = new MessageReceiver(srv);
            

            IObservable<EventPattern<LogEventArgs>> o = Observable.FromEventPattern<LogEventArgs>(typeof(Program), "onLogEvent");
            IObservable<long> oneSecondBufferOpenings;
            Func<long, IObservable<long>> fiveSecondsBufferClosingSelector;
            MovingWindowSequenceGenerator.Generate(1000, 1000*5, out oneSecondBufferOpenings, out fiveSecondsBufferClosingSelector);
            

            var openEverySecond = Observable.Generate(1, x => true, x => x + 1, x => x, x => TimeSpan.FromSeconds(x <= 60*5 ? 0 : 1));
            var seq = Observable.Generate((long) 1, x => true, x => x + 1, x => x, x => TimeSpan.FromSeconds(x <= 60*5 ? 0 : 1));
            var openEveryFiveMinutes = Observable.Generate(1, x => true, x => x + 1, x => x, x => TimeSpan.FromSeconds(x <= 60*5 ? 0 : 60*5));
            
            IObservable<IList<EventPattern<LogEventArgs>>> everySecond = o.Buffer(openEverySecond, _ => GetWindowTimeout()).Select(w => w.Where(c => c.EventArgs.LogEvent.Counter == "RN_Sent").ToList());
            IObservable<IList<EventPattern<LogEventArgs>>> everyFiveMinutes = o.Buffer(openEveryFiveMinutes, _ => GetWindowTimeout()).Select(w => w.Where(c => c.EventArgs.LogEvent.Counter == "RN_Sent").ToList());

            List<string> servers = new List<string>()
                                       {
                                           "APP103",
                                           "APP104",
                                           "APP105",
                                           "APP106",
                                           "APP107",
                                           "APP108",
                                           "APP114",
                                           "APP121",
                                           "APP124"
                                       };
            //_onNextDelegate = CreateOnNext("catFilter", "InstanceFilter");
            everySecond.Subscribe(onNext: OnNext);
            everySecond.Subscribe(
                a =>
                Console.WriteLine("За последние 5 минут сдано {0} отчетов на фронтах: {1}",
                                  a.Count == 0
                                      ? 0
                                      : a.Sum(e => Int32.Parse(e.EventArgs.LogEvent.Value)),
                                  String.Join(", ",
                                              a.GroupBy(k => k.EventArgs.LogEvent.Source,
                                                        (k => Int32.Parse(k.EventArgs.LogEvent.Value))).Select(
                                                            s => s.Key + ": " + s.Sum()))));
            double expectedBeDistribution = 100 /(double) servers.Count;
            everySecond.Subscribe(a =>
                                      {
                                          int total = a.Sum(e => Int32.Parse(e.EventArgs.LogEvent.Value));
                                          CultureInfo enGbCulture = CultureInfo.CreateSpecificCulture("en-GB");
                                          Console.WriteLine(
                                          //writer.WriteLine(
                                              String.Format("{0}\t{1}\t{2}\t{3}", DateTime.Now.Ticks, DateTime.Now.Ticks,
                                                            total,
                                                            String.Join("\t",
                                                                        servers.Select(
                                                                            s =>
                                                                                {
                                                                                    double count = a.Where(
                                                                                        e =>
                                                                                        e.EventArgs.LogEvent.Source == s)
                                                                                        .
                                                                                        Select(
                                                                                            e =>
                                                                                            Double.Parse(
                                                                                                e.EventArgs.LogEvent.
                                                                                                    Value))
                                                                                        .Sum();
                                                                                    return count == 0
                                                                                               ? (-expectedBeDistribution)
                                                                                               : ((count*100)/total) -
                                                                                                 expectedBeDistribution;
                                                                                }
                                                                            ).Select(
                                                                                x => x.ToString("0.000", enGbCulture)))));

                                          //writer.Flush();
                                      });

            everyFiveMinutes.Subscribe(a =>
            {
                int total = a.Sum(e => Int32.Parse(e.EventArgs.LogEvent.Value));
                CultureInfo enGbCulture = CultureInfo.CreateSpecificCulture("en-GB");
                Console.WriteLine(
                //writer2.WriteLine(
                    String.Format("{0}\t{1}\t{2}\t{3}", DateTime.Now.Ticks, DateTime.Now.Ticks,
                                  total,
                                  String.Join("\t",
                                              servers.Select(
                                                  s =>
                                                  {
                                                      double count = a.Where(
                                                          e =>
                                                          e.EventArgs.LogEvent.Source == s)
                                                          .
                                                          Select(
                                                              e =>
                                                              Double.Parse(
                                                                  e.EventArgs.LogEvent.
                                                                      Value))
                                                          .Sum();
                                                      return count == 0
                                                                 ? (-expectedBeDistribution)
                                                                 : ((count * 100) / total) -
                                                                   expectedBeDistribution;
                                                  }
                                                  ).Select(
                                                      x => x.ToString("0.000", enGbCulture)))));

                //writer2.Flush();
            });

            Console.ReadKey();

        }

        private static OnNextDelegate CreateSumDelegate(string catfilter, string instancefilter)
        {
            return
                events =>
                    {
                        var  filtered = events.Where(e => String.IsNullOrEmpty(catfilter)
                                              ? true
                                              : e.EventArgs.LogEvent.Category == catfilter
                                                && String.IsNullOrEmpty(instancefilter)
                                                    ? true
                                                    : e.EventArgs.LogEvent.Instance == instancefilter);

                        Console.WriteLine("{0}{1} SUM: {2}", catfilter,
                                          String.IsNullOrEmpty(instancefilter) ? "" : "@" + instancefilter,
                                          filtered.Sum(e => Double.Parse(e.EventArgs.LogEvent.Value)));
                    };
        }

        private static void OnNext(IList<EventPattern<LogEventArgs>> obj)
        {
            throw new NotImplementedException();
        }


        static void srv_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream(e.Message.MessageData);
            LogEvent le = (LogEvent) bf.Deserialize(ms);
            InvokeOnLogEvent(new LogEventArgs(le));
        }
       
        static void WriteToConsole(object sender, LogEventArgs e)
        {
            Console.WriteLine(e.LogEvent);
        }

        private static int i;
        static object _locker = new object();

        private static IObservable<long> GetWindowTimeout()
        {
            if (i < 60*5)
                lock (_locker)
                {
                    if (i < 60*5)
                        lock (_locker)
                            i++;
                    
                }
            var close = Observable.Timer(TimeSpan.FromSeconds(i));
            return close;
        }

        
    }

    internal static class MovingWindowSequenceGenerator
    {
        public static void Generate(int moveEvery, int windowLength, out IObservable<long> openWindowSequence, out Func<long, IObservable<long>> closingWindowSequenceSelector)
        {
            openWindowSequence = Observable.Generate((long)1, x => true, x => x + 1, x => x, x => TimeSpan.FromMilliseconds(x *moveEvery <= windowLength ? 0 : moveEvery));
            closingWindowSequenceSelector = delegate (long i)
                                                {
                                                    long actualLength = i*moveEvery <= windowLength ? i*moveEvery : windowLength;
                                                    return Observable.Timer(TimeSpan.FromMilliseconds(actualLength));
                                                };

        }
    }
}
