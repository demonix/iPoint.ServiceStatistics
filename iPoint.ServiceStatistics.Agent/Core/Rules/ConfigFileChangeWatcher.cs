using System;
using System.IO;
using System.Threading;
using NLog;

namespace iPoint.ServiceStatistics.Agent.Core.Rules
{
    public class ConfigFileChangeWatcher: IDisposable
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly Action<string> _onChanged;
        private readonly Action<string, string> _onRenamed;
        private FileSystemWatcher _changeMonitor;
        private Timer _reloadTimer;
        private const int ReconfigAfterFileChangedTimeout = 1000;


        public ConfigFileChangeWatcher(string fileName, Action<string> onChanged, Action<string, string> onRenamed)
        {
            _onChanged = onChanged;
            _onRenamed = onRenamed;
            _changeMonitor = new FileSystemWatcher(Path.GetDirectoryName(fileName), Path.GetFileName(fileName));
            _changeMonitor.NotifyFilter = NotifyFilters.LastWrite;
            _changeMonitor.Changed += SheduleChanged;
            _changeMonitor.Renamed += Renamed;
        }


        public void EnableWatching()
        {
            _changeMonitor.EnableRaisingEvents = true;
        }

        private void Renamed(object sender, RenamedEventArgs e)
        {
            _logger.Info("config " + e.OldFullPath + " renamed to " + e.FullPath);
            _onRenamed(e.OldFullPath, e.FullPath);
        }

        private void SheduleChanged(object sender, FileSystemEventArgs e)
        {
            _logger.Info("config " + e.FullPath + " changed");
            lock (this)
            {
               if (_reloadTimer == null)
                {
                    _reloadTimer = new Timer(
                        Changed,
                        e,
                        ReconfigAfterFileChangedTimeout,
                        Timeout.Infinite);
                }
                else
                {
                    _reloadTimer.Change(ReconfigAfterFileChangedTimeout, Timeout.Infinite);
                }
            }
        }

        private void Changed(object state)
        {
            _reloadTimer = null;
            FileSystemEventArgs e = (FileSystemEventArgs) state;
            _onChanged(e.FullPath);
        }

        public void Dispose()
        {
            _changeMonitor.Dispose();
            if (_reloadTimer != null)
            {
                _reloadTimer.Dispose();
                _reloadTimer = null;
            }
        }
    }
}