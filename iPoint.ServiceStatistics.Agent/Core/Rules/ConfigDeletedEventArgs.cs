using System;

namespace iPoint.ServiceStatistics.Agent.Core.Rules
{
    public class ConfigDeletedEventArgs : EventArgs
    {
        public string ConfigPath { get; private set; }

        public ConfigDeletedEventArgs(string configPath)
        {
            ConfigPath = configPath;
        }
    }
}