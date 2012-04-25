using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using NLog;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;
using iPoint.ServiceStatistics.Agent.Core.LogFiles;
using MyLib.ClientSide.Networking;
using iPoint.ServiceStatistics.Agent.Core.Rules;
using LogDescription = iPoint.ServiceStatistics.Agent.Core.LogEvents.LogDescription;

namespace iPoint.ServiceStatistics.Agent
{
    internal class Program
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static Settings _settings;
        private static TcpClient _tcpClient;


        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogManager.GetCurrentClassLogger().Fatal("Unhandled Exception was thrown\r\n" + e.ExceptionObject);
        }

        private static bool _debug;

        private static void Main(string[] args)
        {
            LogDescriptionManager logDescriptionManager = new LogDescriptionManager(".\\settings\\LogDescriptions\\");
            Console.ReadKey();
        }

        private static void OldMain(string[] args)
        {
            
            _logger.Info("Starting App...");
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            if (args.Length > 0 && args[0].ToLower() == "debug")
                _debug = true;

            if (!_debug)
                _tcpClient = new TcpClient(new IPEndPoint(IPAddress.Any, 0),
                                           new IPEndPoint(IPAddress.Parse(args[0]), Int32.Parse(args[1])));
            _settings = new Settings();
            _logger.Info("Total active log descriptions: " + _settings.LogDescriptions.Count);
            foreach (LogDescription ld in _settings.LogDescriptions)
            {
                WatchOnLogDescription(ld);
            }

            _settings.LogDescriptionChangeDetected += OnLogDescriptionChangeDetected;

            Console.ReadKey();
        }

        private static void OnLogDescriptionChangeDetected(object sender,
                                                           LogDescriptionChangeEventArgs logDescriptionChangeEventArgs)
        {
            _logger.Info(String.Format("log descriptions settings {0} detected",
                                       logDescriptionChangeEventArgs.ChangeType));
            _logger.Info("Total active log descriptions now: " + _settings.LogDescriptions.Count);
            LogDescription ld = logDescriptionChangeEventArgs.LogDescription;
            if (logDescriptionChangeEventArgs.ChangeType == ChangeType.Created)
                WatchOnLogDescription(ld);
            else if (logDescriptionChangeEventArgs.ChangeType == ChangeType.Deleted)
                StopWatchOnLogDescription(ld);
        }

        private static void StopWatchOnLogDescription(LogDescription ld)
        {
            _watchers[ld.Id].Dispose();
            _watchers.Remove(ld.Id);
        }

        private static Dictionary<string, LogWatcher> _watchers = new Dictionary<string, LogWatcher>();

        private static void WatchOnLogDescription(LogDescription ld)
        {
            LogEventEvaluator eventEvaluator = new LogEventEvaluator(ld.LogEventDescriptions);
            LogWatcher logWatcher = new LogWatcher(ld);

            if (_debug)
                logWatcher.OnEventFromReader +=
                    (s, e) =>
                        { foreach (var ev in eventEvaluator.Evaluate(e.LogFileName, e.Line)) OutToConsole(null, ev); };
            else
            {
                logWatcher.OnEventFromReader +=
                    (s, e) =>
                        { foreach (var ev in eventEvaluator.Evaluate(e.LogFileName, e.Line)) CountEvents(null, ev); };
                logWatcher.OnEventFromReader +=
                    (s, e) =>
                        { foreach (var ev in eventEvaluator.Evaluate(e.LogFileName, e.Line)) OutToServer(null, ev); };
            }
            _watchers.Add(ld.Id, logWatcher);
        }

        private static int _totalEvents;
        private static DateTime _lastCountEventsCheckPoint = DateTime.Now;
        private static object _eLock = new object();

        private static void CountEvents(object sender, LogEvent e)
        {
            _logger.Debug(e);
            if (_lastCountEventsCheckPoint.AddMinutes(5) > DateTime.Now)
                Interlocked.Increment(ref _totalEvents);
            else
            {
                lock (_eLock)
                {
                    if (_lastCountEventsCheckPoint.AddMinutes(5) <= DateTime.Now)
                    {
                        _logger.Info(_totalEvents + " events generated");
                        _totalEvents = 0;
                        _lastCountEventsCheckPoint = DateTime.Now;
                    }
                }
            }
        }

        private static void OutToServer(object sender, LogEvent e)
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, e);
            ms.Seek(0, SeekOrigin.Begin);
            byte[] data = new byte[ms.Length];
            ms.Read(data, 0, data.Length);
            try
            {
                _tcpClient.Send(new MessagePacket(data).GetBytesForTransfer());
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error while sending", ex);
            }
        }

        private static void OutToConsole(object sender, LogEvent e)
        {
            Console.WriteLine(e);
        }
    }
}
