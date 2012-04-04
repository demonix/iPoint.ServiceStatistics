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

namespace iPoint.ServiceStatistics.Agent
{
    class Program
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static List<LogDescription> _logDescriptions;
        private static Dictionary<string,LogEventMatcher> _logEventMatchers = new Dictionary<string, LogEventMatcher>();
        private static Dictionary<string, ILogReader> _logReaders = new Dictionary<string, ILogReader>();
        private static Settings _settings;
        private static TcpClient _tcpClient;


        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogManager.GetCurrentClassLogger().Fatal("Unhandled Exception was thrown\r\n" + e.ExceptionObject);
        }

        static void Main(string[] args)
        {
            
            _logger.Info("Starting App...");
           AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            _tcpClient = new TcpClient(new IPEndPoint(IPAddress.Any, 0), new IPEndPoint(IPAddress.Parse(args[0]), Int32.Parse(args[1])));
            _settings = new Settings();
            _logDescriptions = _settings.LogDescriptions;
            List<LogWatcher> lw = new List<LogWatcher>();
            _logger.Info("Total active log descriptions: " + _logDescriptions.Count);
            foreach (LogDescription logDescription in _logDescriptions)
            {
                _logger.Info("Creating watchers for " + logDescription.ConfigFileName);
                if (!_logEventMatchers.ContainsKey(logDescription.ConfigFileName))
                    _logEventMatchers.Add(logDescription.ConfigFileName, new LogEventMatcher(logDescription.LogEventDescriptions));
                LogWatcher logWatcher = new LogWatcher(logDescription.ConfigFileName, logDescription.LogDirectory, logDescription.FileMask);
                logWatcher.NewLogFileCreated += logWatcher_NewLogFileCreated;
                logWatcher.LogFileCompleted += logWatcher_LogFileCompleted;
                //TODO: dirty hack
                CreateReadersForCurrentLogs(logDescription, logWatcher);
            }
            Console.ReadKey();


        }


        static void CreateReadersForCurrentLogs(LogDescription logDescription, LogWatcher logWatcher)
        {
            DateTime now = DateTime.Now;
            FileInfo[] files = new DirectoryInfo(logDescription.LogDirectory).GetFiles();
            _logger.Info("Found " + files.Length + " files in log directory " + logDescription.LogDirectory);
            foreach (FileInfo fileInfo in files)
            {
                
               if (logDescription.FileMask.IsMatch(fileInfo.Name) && fileInfo.LastWriteTime >= now.Date)
               {
                   _logger.Info("File " + fileInfo.FullName + " complies conditions");
                   CreateNewReader(fileInfo.FullName, fileInfo.Length, _logEventMatchers[logWatcher.Id]);
               }
               else
               {
                   _logger.Info("File " + fileInfo.FullName + " does not comply conditions");
               }
            }

            

        }

        static void logWatcher_LogFileCompleted(object sender, LogWatcherEventArgs e)
        {
            _logger.Info("Log file readed to end: "+e.FullPath);
            try
            {
                _logReaders[e.FullPath].Close();
                _logReaders[e.FullPath].LineReaded -= OutToConsole;
                _logReaders[e.FullPath].LineReaded -= OutToServer;
                _logReaders[e.FullPath] = null;
                _logReaders.Remove(e.FullPath);
            }catch(Exception ex)
            {
                _logger.FatalException(String.Format("Ошибка при завершении обработки файла {0}", e.FullPath), ex);
                throw;
            }
        }

         static void CreateNewReader(string filePath, long position, LogEventMatcher logEventMatcher)
         {
             _logger.Info("Begin reading of " + filePath);
             ILogReader lr = new TextLogReader(filePath, position, Encoding.Default, null, logEventMatcher);
             //lr.OnLogEvent += OutToConsole;
             lr.OnLogEvent += OutToServer;
             lr.OnLogEvent += CountEvents;

             lr.BeginRead();
             _logReaders.Add(filePath, lr);
         }

        private static int _totalEvents;
        private static DateTime _lastCountEventsCheckPoint = DateTime.Now;
        private static object _eLock = new object();

        private static void CountEvents(object sender, LogEventArgs e)
        {
            _logger.Debug(e.LogEvent);
            if (_lastCountEventsCheckPoint.AddMinutes(5) > DateTime.Now)
                Interlocked.Increment(ref _totalEvents);
            else
            {
                lock (_eLock)
                {
                    if (_lastCountEventsCheckPoint.AddMinutes(5) <= DateTime.Now)
                    {
                        _logger.Info(_totalEvents +" events generated");
                        _totalEvents = 0;
                        _lastCountEventsCheckPoint = DateTime.Now;
                    }
                }
            }
        }

        private static void OutToServer(object sender, LogEventArgs e)
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, e.LogEvent);
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

        private static void OutToConsole(object sender, LogEventArgs e)
        {
                Console.WriteLine(e.LogEvent);
        }

        static void CreateNewReader(string filePath, LogEventMatcher logEventMatcher)
        {
            CreateNewReader(filePath, 0, logEventMatcher);
        }

        static void logWatcher_NewLogFileCreated(object sender, LogWatcherEventArgs e)
        {
            _logger.Info("New log file creation detected: "+e.FullPath);
            LogWatcher lw = (LogWatcher) sender;
            CreateNewReader(e.FullPath, _logEventMatchers[lw.Id]);
        }

        private static void OutToServer(object sender, LineReadedEventArgs e)
        {
            string line = e.Line;
            string logFileName = e.LogFileName;
            LogEventMatcher matcher = e.LogEventMatcher;
            foreach (LogEvent logEvent in matcher.FindMatches(logFileName, line))
            {
                MemoryStream ms = new MemoryStream();
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, logEvent);
                ms.Seek(0, SeekOrigin.Begin);
                byte[] data = new byte[ms.Length];
                ms.Read(data, 0, data.Length);
                try
                {
                    _tcpClient.Send(new MessagePacket(data).GetBytesForTransfer());
                }
                catch(Exception ex)
                {
                    _logger.ErrorException("Error while sending", ex);
                }
            }
        }

        private static void OutToConsole(object sender, LineReadedEventArgs e)
        {
            string line = e.Line;
            string logFileName = e.LogFileName;
            LogEventMatcher matcher = e.LogEventMatcher;
            foreach (LogEvent logEvent in matcher.FindMatches(logFileName, line))
            {
                Console.WriteLine(logEvent);
            }
        }
    }
}
