using System;

namespace iPoint.ServiceStatistics.Agent.Core.Rules
{
    public class ConfigCreatedEventArgs : EventArgs
    {
        public string ConfigPath { get; private set; }

        public ConfigCreatedEventArgs(string configPath)
        {
            ConfigPath = configPath;
        }
    }
}