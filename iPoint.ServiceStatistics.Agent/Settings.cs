using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NLog;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;

namespace iPoint.ServiceStatistics.Agent
{
    public class Settings
    {
        public List<LogDescription> LogDescriptions;
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public List<LogWatcher> LogWatchers = new List<LogWatcher>();
        public event EventHandler<LogDescriptionChangeEventArgs> LogDescriptionChangeDetected;
        private FileSystemWatcher _fsWatcher;


        private void OnLogDescriptionChangeDetected(LogDescriptionChangeEventArgs e)
        {
            EventHandler<LogDescriptionChangeEventArgs> handler = LogDescriptionChangeDetected;
            if (handler != null) handler(this, e);
        }

        public Settings()
        {
            LogDescriptions = new List<LogDescription>();
            GetLogDescrptions();
            _fsWatcher = new FileSystemWatcher(new DirectoryInfo("./settings/LogDescriptions").FullName, "*.logDescription");
            _fsWatcher.Changed += OnChanged;
            _fsWatcher.Created += OnChanged;
            _fsWatcher.Deleted += OnChanged;
            _fsWatcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            switch (fileSystemEventArgs.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    {
                        AddLogDescriptions(LogDescription.ProcessLogDescription(fileSystemEventArgs.FullPath));
                        break;

                    }
                case WatcherChangeTypes.Changed:
                    {
                        UpdateLogDescriptions(LogDescription.ProcessLogDescription(fileSystemEventArgs.FullPath));
                        break;
                    }
                case WatcherChangeTypes.Deleted:
                    {
                        RemoveLogDescriptions(LogDescription.ProcessLogDescription(fileSystemEventArgs.FullPath));
                        break;
                    }
            }
        }

        private void AddLogDescriptions(LogDescription logDescription)
        {
            _logger.Info("log descr. Created!");
            if (!LogDescriptions.Contains(logDescription))
            {
                LogDescriptions.Add(logDescription);
                OnLogDescriptionChangeDetected(new LogDescriptionChangeEventArgs(logDescription, ChangeType.Created));
            }
        }

        private void RemoveLogDescriptions(LogDescription logDescription)
        {
            _logger.Info("log descr. Removed!");
            if (LogDescriptions.Contains(logDescription))
            {
                LogDescriptions.Remove(logDescription);
                OnLogDescriptionChangeDetected(new LogDescriptionChangeEventArgs(logDescription, ChangeType.Deleted));
            }
        }

        private void UpdateLogDescriptions(LogDescription logDescription)
        {
            _logger.Info("log descr. Cahnge! Remove and add it back");
            RemoveLogDescriptions(logDescription);
            AddLogDescriptions(logDescription);
        }

        private void GetLogDescrptions()
        {
            List<LogDescription> result = new List<LogDescription>();
            FileInfo[] fileInfos = new DirectoryInfo("./settings/LogDescriptions").GetFiles("*.logDescription");
            foreach (FileInfo fileInfo in fileInfos)
            {
                _logger.Info("Processing log description from " + fileInfo.FullName);
                UpdateLogDescriptions(LogDescription.ProcessLogDescription(fileInfo.FullName));
            }
        }
    }

    public class LogDescriptionChangeEventArgs : EventArgs
    {
        public LogDescription LogDescription { get; private set; }
        public ChangeType ChangeType { get; private set; }

        public LogDescriptionChangeEventArgs(LogDescription logDescription, ChangeType changeType)
        {
            LogDescription = logDescription;
        }
    }

    public enum ChangeType
    {
        Deleted,
        Created,
        Changed
    }
}
