using System;
using System.IO;
using NLog;

namespace iPoint.ServiceStatistics.Agent.Core.Rules
{
    public abstract class ChangeMonitorableConfigsDirectory : IDisposable
    {
        protected abstract void OnConfigAdded(string filePath);
        protected abstract void OnConfigRemoved(string filePath);
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private FileSystemWatcher _configDirWatcher;


        protected ChangeMonitorableConfigsDirectory(string directoryPath, string filter)
        {
            _configDirWatcher = new FileSystemWatcher(new DirectoryInfo(directoryPath).FullName, filter);
            _configDirWatcher.Created += HandleConfigCreated;
            _configDirWatcher.Deleted += HandleConfigDeleted;
            _configDirWatcher.EnableRaisingEvents = true;
        }

        #region IDisposable Members

        public void Dispose()
        {
            _configDirWatcher.Dispose();
        }

        #endregion

        /*public event EventHandler<ConfigCreatedEventArgs> ConfigCreated;

        public void OnConfigCreated(ConfigCreatedEventArgs e)
        {
            EventHandler<ConfigCreatedEventArgs> handler = ConfigCreated;
            if (handler != null) handler(this, e);
        }

        public event EventHandler<ConfigDeletedEventArgs> ConfigDeleted;

        public void OnConfigDeleted(ConfigDeletedEventArgs e)
        {
            EventHandler<ConfigDeletedEventArgs> handler = ConfigDeleted;
            if (handler != null) handler(this, e);
        }
*/

        private void HandleConfigDeleted(object sender, FileSystemEventArgs e)
        {
            _logger.Info("config deleted: " + e.FullPath);
            //OnConfigDeleted(new ConfigDeletedEventArgs(e.FullPath));
            OnConfigRemoved(e.FullPath);
        }

        private void HandleConfigCreated(object sender, FileSystemEventArgs e)
        {
            _logger.Info("config created: " + e.FullPath);
            if (!File.Exists(e.FullPath)) return;
            //OnConfigCreated(new ConfigCreatedEventArgs(e.FullPath));
            OnConfigAdded(e.FullPath);
        }
    }
}