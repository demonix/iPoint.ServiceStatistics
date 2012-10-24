using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aggregation;
using AggregationEx;
using EventEvaluationLib;
using MyLib.Networking;
using iPoint.ServiceStatistics.Server;
using AggregatorSettings = Aggregation.Experimental.AggregatorSettings;
using AggregatorsManager = Aggregation.Experimental.AggregatorsManager;

namespace ExperimentalApp
{
    using Aggregation.Experimental.RxExtensions;

    internal class FooAggregator
    {
        public IObserver<string> PushObserver;

        public IObservable<string> AggredateObservable;

        public FooAggregator()
        {

            AggredateObservable = Observable.Create<string>(
                a =>
                    {
                        Console.WriteLine("Created in AggredateObservable");
                        a.OnNext("OnNext from Created in AggredateObservable");
                        return Disposable.Empty;
                    });
            //AggredateObservable = Observable.
            PushObserver = Observer.Create<string>(_ => { });
        }
    }

    internal class Program
    {
        private static void WriteLineAndWaitForEnter(string msg)
        {
            Console.WriteLine(msg);
            Console.ReadLine();
        }



        private static void Test13()
        {
            ConcurrentDictionary<AggregationKey, List<string>> dic = new ConcurrentDictionary<AggregationKey, List<string>>();
            DateTime d1 = DateTime.Now.RoundTo(TimeSpan.FromSeconds(1));
            DateTime d2 = d1;
            DateTime d3 = d2.AddSeconds(5);

            Dictionary<string, string> dic1 = new Dictionary<string, string>();
            dic1["e1"] = "v1";
            dic1["e2"] = "v2";
            dic1["e3"] = "v3";
            dic1["e4"] = "v4";
            Dictionary<string, string> dic2 = new Dictionary<string, string>();
            dic2["e1"] = "v1";
            dic2["e2"] = "v2";
            dic2["e3"] = "v3";
            dic2["e4"] = "v5";
            Dictionary<string, string> dic3 = new Dictionary<string, string>();
            dic3["e1"] = "v1";
            dic3["e2"] = "v2";
            dic3["e3"] = "v3";
            dic3["e4"] = "v4";
            AggregationKey ak1 = new AggregationKey(d1,dic1);
            AggregationKey ak2 = new AggregationKey(d2,dic2);
            AggregationKey ak3 = new AggregationKey(d3,dic3);
            dic.GetOrAdd(ak1,new List<string>()).Add("f1");
            dic.GetOrAdd(ak2,new List<string>()).Add("f2");
            dic.GetOrAdd(ak3,new List<string>()).Add("f3");
            Console.ReadLine();
        }

        private static void Main(string[] args)
        {
            LogEvent le = new LogEvent(EventType.Counter, DateTime.Now,"1","1","2","3","","4");
            for (int i = 0; i < 10; i++)
            {
                OutToHttpServer(null, le);    
            }
            
            /*Test12();
            Test14();*/
            Console.WriteLine("Done");
            Console.ReadLine();
        }


        private static void Test14()
        {
            DataLayerEx.Database.InitConnection("localhost", null, "test2");

            var result = DataLayerEx.Database.Instance.Find(DateTime.MinValue, DateTime.MaxValue,
                                                            //new Dictionary<string, List<string>>() {{"cCat", new List<string>{"SomeCategory0"}}, {"cName", new List<string>()}}
                                                            new Dictionary<string, List<string>>()
                                                            );
            Stopwatch sw = Stopwatch.StartNew();
            var r = result.ToList();
            sw.Stop();
            Console.WriteLine("Done in " + sw.ElapsedMilliseconds+ " ms");
            Console.ReadLine();
        }

        //static Random rnd = new Random();
        static List<string> _categories = new List<string>();
        static List<string> _counterNames = new List<string>();
        static List<string> _sources = new List<string>();
        static List<string> _instances = new List<string>();
        static List<string> _extDatas = new List<string>() ;

        static LogEvent GenerateLogEvent()
        {
            Random rnd = RandomProvider.GetThreadRandom();
            string source = _sources[rnd.Next(0, _sources.Count)];
            string instance = _instances[rnd.Next(0, _instances.Count)];
            string extData = _extDatas[rnd.Next(0, _extDatas.Count)];
            string category = _categories[rnd.Next(0, _categories.Count)];
            string counterName = _counterNames[rnd.Next(0, _counterNames.Count)];
            return new LogEvent(EventType.Counter, DateTime.Now, source, category, counterName, instance, extData,
                                rnd.Next(100).ToString());
        }

        static ConcurrentBag<string> log = new ConcurrentBag<string>();
        static ConcurrentBag<string> resultlog = new ConcurrentBag<string>();

        private static AggregationEx.Aggregator aggregator;
        private static void Test12()
        {
            for (int i = 0; i < 2; i++)
            {
                _categories.Add("SomeCategory"+i);
            }
            for (int i = 0; i < 2; i++)
            {
                _counterNames.Add("SomeName" + i);
            }
            for (int i = 0; i < 50; i++)
            {
                _sources.Add("Source" + i);
            }
            
            foreach (var v in Enumerable.Range(1, 5))
                _instances.Add("");
            foreach (var v in Enumerable.Range(1, 100))
                _extDatas.Add("");
            
            
            for (int i = 0; i < 2; i++)
            {
                _instances.Add("Instance" + i);
            }
            for (int i = 0; i < 2; i++)
            {
                _extDatas.Add("ExtData" + i);
            }
            //DataLayerEx.Database.InitConnection("localhost",null,"test2");
            aggregator  = new AggregationEx.Aggregator("CounterCategory",
                                                                                          "CounterName",
                                                                                          new List<string>() { "Count", "Avg", "Max","Min","Sum", "Percentile" },
                                                                                          new Dictionary<string, string>
                                                                                              ()
                                                                                              {
                                                                                                  {
                                                                                                      "Percentile",
                                                                                                      "20|50|90|99"
                                                                                                  }
                                                                                              },
                                                                                          avalue =>
                                                                                              {
                                                                                                  //Console.WriteLine(
                                                                                                  //    avalue.ToString());
                                                                                                  //resultlog.Add(
                                                                                                  //    avalue.ToString());
                                                                                                  DataLayerEx.Database.Instance.Save(avalue);
                                                                                              });

            int threadsCount = 1;// Environment.ProcessorCount + 2;
            
            List<Thread> threads = new List<Thread>(threadsCount);
            Thread flushlog = new Thread(FlushLog);
            flushlog.Start();

            for (int j = 0; j < threadsCount; j++)
            {
                Thread th = new Thread(DoWork);
                th.Priority = ThreadPriority.Lowest;
                threads.Add(th);
                th.Start();    
            }
            foreach (var thread in threads)
            {
                thread.Join();
            }

            
            File.WriteAllLines("log", log.ToList());
            File.WriteAllLines("resultLog",resultlog.ToList());
        }

        private static void OutToHttpServer(object sender, LogEvent e)
        {
            byte[] data = e.Serialize();
            Uri url = new Uri("http://lit-karmazin:80/Temporary_Listen_Addresses/postbinary");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ServicePoint.Expect100Continue = false;
            request.ServicePoint.ConnectionLimit = 2;
            request.ServicePoint.UseNagleAlgorithm = false;
            request.KeepAlive = true;
            request.Method = "POST";
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Flush();
            requestStream.Close();
            
            try
            {
                request.GetResponse();
                requestStream.Dispose();
               
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);
            }

        }
        static void FlushLog()
        {
            while (true)
            {
                ConcurrentBag<string> newLog = new ConcurrentBag<string>();
                var tmp = Interlocked.Exchange(ref log, newLog);
                File.AppendAllLines("tmp", tmp.ToList());
                Thread.Sleep(1000*60);
            }
            
        }
        static Stopwatch stopwatch = Stopwatch.StartNew();
        private static int eventCount = 0;
      static void DoWork ()
      {
          for (int cnt = 0; cnt < 800000; cnt++)
          {
              eventCount++;
              var evt = GenerateLogEvent();
              aggregator.Push(evt);
              if (eventCount % 10000 == 0)
              {
                  Console.WriteLine(stopwatch.ElapsedMilliseconds + " per 10000 events");
                  stopwatch.Restart();
              }
              //Console.WriteLine(evt.ToString());
              log.Add(evt.ToString());
              Thread.SpinWait(1);
          }
      }

        

    }

    public static class RandomProvider
    {
        private static int seed = Environment.TickCount;

        private static ThreadLocal<Random> randomWrapper = new ThreadLocal<Random>(() =>
            new Random(Interlocked.Increment(ref seed))
        );

        public static Random GetThreadRandom()
        {
            return randomWrapper.Value;
        }
    }
}
