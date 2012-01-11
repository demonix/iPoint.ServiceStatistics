using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace iPoint.ServiceStatistics.Agent
{
    internal class LogWatcher
    {
        private FileSystemWatcher _fsWatcher;
        public Regex FileMaskRegex { get; private set; }
        public event EventHandler<LogWatcherEventArgs> NewLogFileCreated;
        public event EventHandler<LogWatcherEventArgs> LogFileCompleted;
        private Timer _timer;
        List<string> _logsUnderWatch = new List<string>();
        public string Id { get; private set; }
        public void InvokeLogFileCompleted(LogWatcherEventArgs e)
        {
            EventHandler<LogWatcherEventArgs> handler = LogFileCompleted;
            if (handler != null) handler(this, e);
        }

        public void InvokeNewLogFileCreated(LogWatcherEventArgs e)
        {
            EventHandler<LogWatcherEventArgs> handler = NewLogFileCreated;
            if (handler != null) handler(this, e);
        }

        public LogWatcher(string id, string directory, Regex fileMask )
        {
            _fsWatcher = new FileSystemWatcher();
            _fsWatcher.Path = directory;
            Id = id;
            FileMaskRegex = fileMask;
            _fsWatcher.Created += OnFileCreated;
            DateTime now = DateTime.UtcNow;
            _timer = new Timer(NewDayHasCome);
            _timer.Change(new DateTime(now.Year, now.Month, now.Day).AddDays(1).AddMinutes(30) - now, TimeSpan.Zero);
            _fsWatcher.EnableRaisingEvents = true;
        }


        private void NewDayHasCome(object state)
        {
            int logsCount = _logsUnderWatch.Count;
            for (int i = logsCount - 1; i >= 0; i--)
            {
                if (new FileInfo(_logsUnderWatch[i]).LastWriteTimeUtc < DateTime.UtcNow.AddMinutes(-30))
                {
                    InvokeLogFileCompleted(new LogWatcherEventArgs(_logsUnderWatch[i]));
                    _logsUnderWatch.RemoveAt(i);
                }
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (Directory.Exists(e.FullPath)) return;
            string fileName = e.Name;
            if (!FileMaskRegex.IsMatch(fileName)) return;
            _logsUnderWatch.Add(e.FullPath);
            InvokeNewLogFileCreated(new LogWatcherEventArgs(e.FullPath));
        }
    }
}