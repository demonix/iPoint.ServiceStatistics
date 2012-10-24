using System;
using System.IO;
using System.Net;
using System.Reactive.Linq;
using Aggregation;
using CountersDataLayer;
using MyLib.Networking;

namespace iPoint.ServiceStatistics.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncTcpServer srv = new AsyncTcpServer(new IPEndPoint(IPAddress.Any, 50001));
            srv.Start();
            MessageReceiver receiver = new MessageReceiver(srv);
            MovingWindowSequence seq = new MovingWindowSequence(1000*60, 1000*5*60);
            string mongoUrl = File.ReadAllText("settings\\mongoConnection");
            //CountersDatabase.InitConnection(mongoUrl);
            var observableEvents = receiver.ObservableEvents.Buffer(seq.BufferOpenings, seq.ClosingWindowSequenceSelector).Publish();
            //observableEvents.Connect();
            Settings settings = new Settings();
            settings.ObservableEvents = observableEvents;
            settings.ReadAggregators();
            CountersAutoDiscoverer countersAutoDiscoverer = new CountersAutoDiscoverer(receiver.ObservableEvents, settings);
            countersAutoDiscoverer.StartDiscovery();
            
            CounterDumper counterDumper = new CounterDumper(receiver.ObservableEvents);
            CounterForwarder counterForwarder = new CounterForwarder(receiver.ObservableEvents);

            DeadCountersDetector deadCountersDetector = new DeadCountersDetector(receiver.ObservableEvents, settings);
            
            observableEvents.Subscribe(l => Console.WriteLine("Total events: " + l.Count));
            ConsoleKeyInfo keyInfo;
            while ((keyInfo = Console.ReadKey()).Key !=ConsoleKey.Enter)
            {
                if (keyInfo.Key == ConsoleKey.Add)
                {
                    seq.IncreaseInterval(1000);
                    Console.WriteLine("Interval is {0} ms now", seq.MoveEvery);
                }
                if (keyInfo.Key == ConsoleKey.Subtract)
                {
                    seq.DecreaseInterval(1000);
                    Console.WriteLine("Interval is {0} ms now", seq.MoveEvery);
                }
                if (keyInfo.Key == ConsoleKey.R)
                {
                    settings.ReadAggregators();
                    Console.WriteLine("Aggregators were updated");
                }

                if (keyInfo.Key == ConsoleKey.F)
                {
                    if (counterForwarder.IsForwarding)
                    {
                        counterForwarder.StopForwarding();
                        Console.WriteLine("Forwarding stopped");
                    }
                    else
                    {
                        counterForwarder.StartForwarding();
                        Console.WriteLine("Forwarding started");
                    }
                }
                if (keyInfo.Key == ConsoleKey.D)
                {
                    if (counterDumper.IsDumping)
                    {
                        counterDumper.StopDumping();
                        Console.WriteLine("Counter Dumping Stopped");
                    }
                    else
                    {
                        counterDumper.StartDumping();
                        Console.WriteLine("Counter Dumping Started");
                    }
                }
                if (keyInfo.Key == ConsoleKey.A)
                {
                    File.WriteAllText("deadCounters",deadCountersDetector.GetCounterFreshnessTimeStats());
                    Console.WriteLine("Stats have been written to file deadCounters");
                }
            }
        }
    }
}
