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
    class Program
    {
        static void Main(string[] args)
        {
            AsyncTcpServer srv = new AsyncTcpServer(new IPEndPoint(IPAddress.Any, 50001));
            srv.Start();
            MessageReceiver receiver = new MessageReceiver(srv);
            MovingWindowSequence seq = new MovingWindowSequence(1000*60, 1000*5*60);
            CountersDatabase.InitConnection("127.0.0.1", null, "counters");
            var observableEvents = receiver.ObservableEvents.Buffer(seq.BufferOpenings, seq.ClosingWindowSequenceSelector).Publish();
            observableEvents.Connect();
            Settings settings = new Settings();
            settings.ObservableEvents = observableEvents;
            settings.ReadAggregators();
            CountersAutoDiscoverer countersAutoDiscoverer = new CountersAutoDiscoverer(receiver.ObservableEvents, settings);
            countersAutoDiscoverer.StartDiscovery();
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
            }
        }
    }
}
