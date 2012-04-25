using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NLog;

namespace iPoint.ServiceStatistics.Agent.Core.Rules
{
    public class LogDescriptionManager : ChangeMonitorableConfigsDirectory, IDisposable
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private Dictionary<string, LogDescriptionConfig> _logDescriptions =
            new Dictionary<string, LogDescriptionConfig>();

        private ReaderWriterLockSlim _rwLocker = new ReaderWriterLockSlim();

        public LogDescriptionManager(string directoryPath)
            : base(directoryPath, "*.logDescription")
        {
            FileInfo[] fileInfos = new DirectoryInfo(directoryPath).GetFiles("*.logDescription");
            foreach (FileInfo fileInfo in fileInfos)
            {
                AddLogDescription(fileInfo.FullName, CreateLogDescription(fileInfo.FullName));
            }
        }

        private static LogDescriptionConfig CreateLogDescription(string fullPath)
        {
            LogDescriptionConfig logDescriptionConfig = null;
            try
            {
                FileInfo fileInfo = new FileInfo(fullPath);
                logDescriptionConfig = new LogDescriptionConfig(fileInfo.FullName);
            }
            catch (Exception ex)
            {
                _logger.Error("Cannot create LogDescription from config file " + fullPath + ": " + ex);
            }
            return logDescriptionConfig;
        }
        
        protected override void OnConfigAdded(string filePath)
        {
            AddLogDescription(filePath, CreateLogDescription(filePath));
        }
        protected override void OnConfigRemoved(string filePath)
        {
            RemoveLogDescription(filePath);
        }

        private void AddLogDescription(string fullPath, LogDescriptionConfig logDescriptionConfig)
        {
            if (logDescriptionConfig == null) return;
            _rwLocker.EnterWriteLock();
            try
            {
                if (_logDescriptions.ContainsKey(fullPath))
                {

                    _logger.Warn(String.Format("LogDescription from file {0} already added. Recreating it.", fullPath));
                    return;
                }
                _logDescriptions.Add(fullPath, logDescriptionConfig);
                logDescriptionConfig.ConfigChanged += UpdateLogDescription;
                logDescriptionConfig.ConfigRenamed += RenameLogDescription;
                logDescriptionConfig.WatchForChanges();

            }
            finally
            {
                _rwLocker.ExitWriteLock();
            }
        }

        private void UpdateLogDescription(object sender, ConfigChangedEventArgs e)
        {
            LogDescriptionConfig logDescriptionConfig = CreateLogDescription(e.ConfigPath);
            RemoveLogDescription(e.ConfigPath);
            AddLogDescription(e.ConfigPath, logDescriptionConfig);
        }

        private void RemoveLogDescription(string fullPath)
        {
            _rwLocker.EnterWriteLock();
            try
            {
                if (!_logDescriptions.ContainsKey(fullPath))
                {
                    _logger.Warn(String.Format("LogDescription from file {0} does not exists", fullPath));
                    return;
                }
                _logDescriptions[fullPath].Dispose();
                _logDescriptions.Remove(fullPath);
            }

            finally
            {
                _rwLocker.ExitWriteLock();
            }
        }

        private void RenameLogDescription(object sender, ConfigRenamedEventArgs e)
        {
            _rwLocker.EnterWriteLock();
            try
            {
                _logDescriptions.Add(e.NewFileName, _logDescriptions[e.OldFileName]);
                _logDescriptions.Remove(e.OldFileName);
            }
            finally
            {
                _rwLocker.ExitWriteLock();
            }
        }

        private void RemoveAllLogDescriptions()
        {

            _rwLocker.EnterWriteLock();
            try
            {
                foreach (var key in _logDescriptions.Keys)
                {
                    _logDescriptions[key].Dispose();
                }
                _logDescriptions.Clear();
                _logger.Info("All removed");
            }
            finally
            {
                _rwLocker.ExitWriteLock();
            }
        }

    }
}