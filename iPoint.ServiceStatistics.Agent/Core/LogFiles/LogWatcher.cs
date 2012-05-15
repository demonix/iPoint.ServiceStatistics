using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NLog;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;
using iPoint.ServiceStatistics.Agent.Core.LogFiles;

namespace iPoint.ServiceStatistics.Agent
{
    public class LogWatcher: IDisposable
    {
        private static object _locker = new object();
        private static Dictionary<string, ILogReader> _logReaders = new Dictionary<string, ILogReader>();
        private Dictionary<string, FileSystemWatcher> _fsWatchers = new Dictionary<string, FileSystemWatcher>();
        private Logger _logger = LogManager.GetCurrentClassLogger();
        private Timer _timer;

        public LogWatcher(LogDescription logDescription)
        {
            LogDescription = logDescription;
            Id = logDescription.Id;
            FileMaskRegex = logDescription.FileMask;
            DateTime now = DateTime.Now;
            _timer = new Timer(NewDayHasCome);
            _timer.Change(new DateTime(now.Year, now.Month, now.Day).AddDays(1).AddMinutes(30) - now,
                          TimeSpan.FromDays(1));
            foreach (string logDirectory in logDescription.LogDirectories)
            {
                CreateReadersForCurrentLogs(logDirectory);    
                AddWatcher(logDirectory);
            }
        }

        private void AddWatcher(string logDirectory)
        {
            lock (_fsWatchers)
            {
                if(_fsWatchers.ContainsKey(logDirectory)) return;
                FileSystemWatcher fsWatcher = new FileSystemWatcher();
                _fsWatchers.Add(logDirectory, fsWatcher);
                fsWatcher.Path = logDirectory;
                fsWatcher.Created += OnFileCreated;
                fsWatcher.EnableRaisingEvents = true;
            }
        }

        private void RemoveWatcher(string logDirectory)
        {
            lock (_fsWatchers)
            {
                if (!_fsWatchers.ContainsKey(logDirectory)) return;
                FileSystemWatcher fsWatcher = _fsWatchers[logDirectory];
                fsWatcher.Dispose();
                _fsWatchers.Remove(logDirectory);
            }
        }

        public LogDescription LogDescription { get; private set; }
        public Regex FileMaskRegex { get; private set; }
        public string Id { get; private set; }


        private void CreateReadersForCurrentLogs(string logDirectory)
        {
            DateTime now = DateTime.Now;
            FileInfo[] files = new DirectoryInfo(logDirectory).GetFiles();
            _logger.Info("Found " + files.Length + " files in log directory " + logDirectory);
            foreach (FileInfo fileInfo in files)
            {
                if (LogDescription.FileMask.IsMatch(fileInfo.Name) && fileInfo.LastWriteTime >= now.Date)
                    CreateNewReader(fileInfo.FullName, fileInfo.Length);
            }
        }

        private void CreateNewReader(string filePath, long position = 0)
        {
            _logger.Info("Begin reading of " + filePath);
            ILogReader lr = new TextLogReader(filePath, position, Encoding.Default, null);
            lr.LineReaded += OnLineReadedFired;
            lock (_locker)
                _logReaders.Add(filePath, lr);
            lr.BeginRead();
        }

        public event EventHandler<LineReadedEventArgs> OnEventFromReader;


        private void OnLineReadedFired(object sender, LineReadedEventArgs lineReadedEventArgs)
        {
            EventHandler<LineReadedEventArgs> handler = OnEventFromReader;
            if (handler != null) handler(this, lineReadedEventArgs);
        }

        private void LogFileCompleted(string fullPath)
        {
            _logger.Info("Log file readed to end: " + fullPath);
            try
            {
                _logReaders[fullPath].Dispose();
                _logReaders.Remove(fullPath);
            }
            catch (Exception ex)
            {
                _logger.FatalException(String.Format("Ошибка при завершении обработки файла {0}", fullPath), ex);
                throw;
            }
        }


        private void NewDayHasCome(object state)
        {
            string[] filePaths = new string[_logReaders.Keys.Count];
            _logReaders.Keys.CopyTo(filePaths,0);

            foreach (string filePath in filePaths)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if ((fileInfo.LastWriteTime < DateTime.Now.Date.AddSeconds(15)) &&
                    (fileInfo.CreationTimeUtc.Date < DateTime.Now.Date))
                {
                    _logger.Info(String.Format("Log file {0} completed", filePath));
                    LogFileCompleted(filePath);
                }
            }

            
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(e.FullPath)) return;
            string fileName = e.Name;
            if (!FileMaskRegex.IsMatch(fileName)) return;
            _logger.Info("New log file creation detected: " + e.FullPath);
            CreateNewReader(e.FullPath);
        }

        public void Dispose()
        {
            OnEventFromReader = null;
            foreach (string key in _logReaders.Keys)
            {
                _logReaders[key].Dispose();                
            }
            _logReaders.Clear();
        }
    }
}