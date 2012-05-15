using System;
using NLog;

namespace iPoint.ServiceStatistics.Agent.Core.Rules
{
    public abstract class ChangeMonitorableConfigFile : IDisposable, IEquatable<ChangeMonitorableConfigFile>
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        protected ConfigFileChangeWatcher ChangeWatcher;

        protected ChangeMonitorableConfigFile(string configFileName)
        {
            ConfigFileName = configFileName;
            ChangeWatcher = new ConfigFileChangeWatcher(configFileName, HandleConfigChange, HandleConfigRename);
        }

        public string ConfigFileName { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            ConfigChanged = null;
            ConfigRenamed = null;
            ChangeWatcher.Dispose();
        }

        #endregion

        public event EventHandler<ConfigChangedEventArgs> ConfigChanged;
        public event EventHandler<ConfigRenamedEventArgs> ConfigRenamed;

        protected void OnConfigRenamed(ConfigRenamedEventArgs e)
        {
            EventHandler<ConfigRenamedEventArgs> handler = ConfigRenamed;
            if (handler != null) handler(this, e);
        }

        protected void OnConfigChanged(ConfigChangedEventArgs e)
        {
            EventHandler<ConfigChangedEventArgs> handler = ConfigChanged;
            if (handler != null) handler(this, e);
        }

        public void WatchForChanges()
        {
            ChangeWatcher.EnableWatching();
        }

        protected void HandleConfigChange(string fileName)
        {
            Type t = this.GetType();
            var arguments = new Object[] {fileName};
            ChangeMonitorableConfigFile newConfig;
            try
            {
                newConfig = (ChangeMonitorableConfigFile) Activator.CreateInstance(t, arguments);
            }
            catch (Exception ex)
            {
                _logger.Info("Config is invalid: " + fileName + "\r\n" + ex);
                return;
            }

            if (this.Equals(newConfig))
            {
                _logger.Info("rule config parameters not changed");
                return;
            }

            _logger.Info("rule config parameters changed");
            OnConfigChanged(new ConfigChangedEventArgs(fileName));
        }

        protected void HandleConfigRename(string oldFileName, string newFileName)
        {
            ConfigFileName = newFileName;
            OnConfigRenamed(new ConfigRenamedEventArgs(oldFileName, newFileName));
        }


        //protected abstract void HandleConfigRename(string oldFileName, string newFileName);
        //protected abstract void HandleConfigChange(string fileName);
        public abstract bool Equals(ChangeMonitorableConfigFile other);
        public new abstract bool Equals(object obj);

    }
}