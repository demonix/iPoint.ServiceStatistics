using System;
using System.IO;
using System.Threading;
using NLog;

namespace EventEvaluationLib
{
    public abstract class ChangeMonitorableConfigStore:IDisposable
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private FileSystemWatcher _changeMonitor;
        private Timer _delayTimer;
        protected string DirectoryToMonitor;
        private int _delayTimeout = 1000;

        protected ChangeMonitorableConfigStore(string directoryToMonitor, string fileMaskToMonitor)
        {
            DirectoryToMonitor = directoryToMonitor;
            if (!Directory.Exists(directoryToMonitor))
            {
                _logger.Error("Directory to watch for does not exists: " + directoryToMonitor);
                return;
            }
            _changeMonitor = new FileSystemWatcher(directoryToMonitor, fileMaskToMonitor);
            _changeMonitor.Changed += OnChangedInternal;
            _changeMonitor.Renamed += OnRenamedInternal;
            _changeMonitor.Created += OnCreatedInternal;
            _changeMonitor.Deleted += OnDeletedInternal;
            _changeMonitor.EnableRaisingEvents = true;
        }

        private void OnDeletedInternal(object sender, FileSystemEventArgs e)
        {
            _logger.Info("config deleted: " + e.FullPath);
            if (File.Exists(e.FullPath)) return;
            ConfigDeleted(e.FullPath);
        }

        private void OnCreatedInternal(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(e.FullPath)) return;
            _logger.Info("config created: " + e.FullPath);
            ConfigCreated(e.FullPath);
        }

        private void OnRenamedInternal(object sender, RenamedEventArgs e)
        {
            _logger.Info("Config file renamed from " + e.OldFullPath + " to " + e.FullPath);
            DirectoryToMonitor = e.FullPath;
            _changeMonitor.Filter = Path.GetFileName(e.FullPath);
            ConfigRenamed(e.OldFullPath, e.FullPath);
        }

        private void OnChangedInternal(object sender, FileSystemEventArgs e)
        {
            _logger.Info("config " + e.FullPath + " changed. waiting a bit");
            lock (this)
            {
                if (_delayTimer == null)
                {
                    _delayTimer = new Timer(
                        DelayConfigChangedEvent,
                        e,
                        _delayTimeout,
                        Timeout.Infinite);
                }
                else
                {
                    _delayTimer.Change(_delayTimeout, Timeout.Infinite);
                }
            }
        }

      

        private void DelayConfigChangedEvent(object state)
        {
            FileSystemEventArgs e = (FileSystemEventArgs) state;
            _logger.Info("executing delayed change action for "+ e.FullPath);
            ConfigChanged(e.FullPath);
        }

        protected abstract void ConfigChanged(string fileName);
        protected abstract void ConfigRenamed(string oldFileName, string newFileName);
        protected abstract void ConfigCreated(string fileName);
        protected abstract void ConfigDeleted(string fileName);

        public virtual void Dispose()
        {
           _changeMonitor.Dispose();
            if (_delayTimer != null)
            {
                _delayTimer.Dispose();
                _delayTimer = null;
            }
        }
    }
}