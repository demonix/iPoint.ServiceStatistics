using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EventEvaluationLib
{
    public class LogWatcherManager : ChangeMonitorableConfigStore
    {
        public event EventHandler<LogEventArgs> EventFromLog;

        Dictionary<string, LogWatcher> _logWatchers = new Dictionary<string, LogWatcher>();

        public LogWatcherManager(string logWatcherConfigDirectory)
            : base(logWatcherConfigDirectory,"*.logWatcherConfig")
        {
            List<String> configFiles = new DirectoryInfo(logWatcherConfigDirectory).GetFiles("*.logWatcherConfig").Select(fi => fi.FullName).ToList();
            foreach (string configFile in configFiles)
            {
                AddWatcher(configFile);
            }
        }

        private void AddWatcher(string configFile)
        {
            LogWatcher watcher = LogWatcher.CreateFromFile(configFile);
            if (watcher != null)
                lock (_logWatchers)
                {
                    _logWatchers.Add(configFile, watcher);
                    watcher.EventFromLog += OnEventFromLog;
                }
        }

        private void OnEventFromLog(object sender, LogEventArgs e)
        {
            var handler = EventFromLog;
            if (handler == null) return;
            handler(sender, e);
        }

        protected override void ConfigChanged(string fileName)
        {
            lock (_logWatchers)
            {
                if (_logWatchers.ContainsKey(fileName))
                {
                    _logWatchers[fileName].Dispose();
                    _logWatchers.Remove(fileName);
                }
                AddWatcher(fileName);
            }
        }

        protected override void ConfigRenamed(string oldFileName, string newFileName)
        {
            lock (_logWatchers)
            {
                if (_logWatchers.ContainsKey(oldFileName))
                {
                    if (Path.GetExtension(newFileName).ToLower() == "logwatcherconfig")
                        _logWatchers.Add(newFileName, _logWatchers[oldFileName]);
                    _logWatchers.Remove(oldFileName);
                }
            }
        }

        protected override void ConfigCreated(string fileName)
        {
            AddWatcher(fileName);
        }

        protected override void ConfigDeleted(string fileName)
        {
            lock (_logWatchers)
            {
                if (_logWatchers.ContainsKey(fileName))
                {
                    _logWatchers[fileName].Dispose();
                    _logWatchers.Remove(fileName);
                }
            }
        }
    }
}