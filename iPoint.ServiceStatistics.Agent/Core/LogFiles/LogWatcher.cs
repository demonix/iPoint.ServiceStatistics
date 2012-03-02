using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using NLog;

namespace iPoint.ServiceStatistics.Agent
{
    internal class LogWatcher
    {
        private Logger _logger = LogManager.GetCurrentClassLogger();
        private FileSystemWatcher _fsWatcher;
        private FileSystemWatcher _fileChangeWatcher;
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
            _fileChangeWatcher = new FileSystemWatcher();
            _fileChangeWatcher.Filter = "";
            Id = id;
            FileMaskRegex = fileMask;
            _fsWatcher.Created += OnFileCreated;
            DateTime now = DateTime.Now;
            _timer = new Timer(NewDayHasCome);
            _timer.Change(new DateTime(now.Year, now.Month, now.Day).AddDays(1).AddMinutes(30) - now, TimeSpan.FromDays(1));
            _fsWatcher.EnableRaisingEvents = true;
        }


        private void NewDayHasCome(object state)
        {
            int logsCount = _logsUnderWatch.Count;
            for (int i = logsCount - 1; i >= 0; i--)
            {
                FileInfo fileInfo = new FileInfo(_logsUnderWatch[i]);
                if ((fileInfo.LastWriteTime < DateTime.Now.Date.AddSeconds(15)) && (fileInfo.CreationTimeUtc.Date < DateTime.Now.Date))
                {
                    _logger.Info(String.Format("Log file {0} completed", _logsUnderWatch[i]) );
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