using System;

namespace iPoint.ServiceStatistics.Agent.Core.Rules
{
    public class ConfigChangedEventArgs : EventArgs
    {
        public string ConfigPath { get; private set; }

        public ConfigChangedEventArgs(string configPath)
        {
            ConfigPath = configPath;
        }
    }
}