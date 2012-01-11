using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;
using iPoint.ServiceStatistics.Agent.Core.LogFiles;
using MyLib.ClientSide.Networking;

namespace iPoint.ServiceStatistics.Agent
{
    class Program
    {
        private static List<LogDescription> _logDescriptions;
        private static Dictionary<string,LogEventMatcher> _logEventMatchers = new Dictionary<string, LogEventMatcher>();
        private static Dictionary<string, ILogReader> _logReaders = new Dictionary<string, ILogReader>();
        private static Settings _settings;
        private static TcpClient _tcpClient;
        static void Main(string[] args)
        {
            _tcpClient = new TcpClient(new IPEndPoint(IPAddress.Any, 0), new IPEndPoint(IPAddress.Parse(args[0]), Int32.Parse(args[1])));
            _settings = new Settings();
            _logDescriptions = _settings.LogDescriptions;
            List<LogWatcher> lw = new List<LogWatcher>();

            foreach (LogDescription logDescription in _logDescriptions)
            {
                if (!_logEventMatchers.ContainsKey(logDescription.ConfigFileName))
                    _logEventMatchers.Add(logDescription.ConfigFileName,
                                          new LogEventMatcher(logDescription.LogEventDescriptions));
                LogWatcher logWatcher = new LogWatcher(logDescription.ConfigFileName, logDescription.LogDirectory,
                                                       logDescription.FileMask);
                logWatcher.NewLogFileCreated += logWatcher_NewLogFileCreated;
                logWatcher.LogFileCompleted += new EventHandler<LogWatcherEventArgs>(logWatcher_LogFileCompleted);
                //TODO: dirty hack
                DateTime now = DateTime.Now;
                string path = Path.Combine(logDescription.LogDirectory,"log"+now.Year.ToString("0000")+"."+now.Month.ToString("00")+"."+now.Day.ToString("00"));
                if (File.Exists(path))
                    CreateNewReader(path, new FileInfo(path).Length, _logEventMatchers[logWatcher.Id]);
            }
            Console.ReadKey();


        }

        static void logWatcher_LogFileCompleted(object sender, LogWatcherEventArgs e)
        {
            _logReaders[e.FullPath].Close();
            _logReaders[e.FullPath].LineReaded -= OutToConsole;
            _logReaders[e.FullPath].LineReaded -= OutToServer;
            _logReaders[e.FullPath] = null;
            _logReaders.Remove(e.FullPath);
        }

         static void CreateNewReader(string filePath, long position, LogEventMatcher logEventMatcher)
         {
             ILogReader lr = new TextLogReader(filePath, position, Encoding.Default, null, logEventMatcher);
             lr.LineReaded += OutToConsole;
             lr.LineReaded += OutToServer;
             lr.BeginRead();
             _logReaders.Add(filePath, lr); 
         }
        static void CreateNewReader(string filePath, LogEventMatcher logEventMatcher)
        {
            CreateNewReader(filePath, 0, logEventMatcher);
        }

        static void logWatcher_NewLogFileCreated(object sender, LogWatcherEventArgs e)
        {
            LogWatcher lw = (LogWatcher) sender;
            CreateNewReader(e.FullPath, _logEventMatchers[lw.Id]);
        }

        private static void OutToServer(object sender, LineReadedEventArgs e)
        {
            string line = e.Line;
            LogEventMatcher matcher = e.LogEventMatcher;
            foreach (LogEvent logEvent in matcher.FindMatches(line))
            {
                MemoryStream ms = new MemoryStream();
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, logEvent);
                ms.Seek(0, SeekOrigin.Begin);
                byte[] data = new byte[ms.Length];
                ms.Read(data, 0, data.Length);
                _tcpClient.Send(new MessagePacket(data).GetBytesForTransfer());
            }
        }

        private static void OutToConsole(object sender, LineReadedEventArgs e)
        {
            string line = e.Line;
            LogEventMatcher matcher = e.LogEventMatcher;
            foreach (LogEvent logEvent in matcher.FindMatches(line))
            {
                Console.WriteLine(logEvent);
            }
        }
    }
}
