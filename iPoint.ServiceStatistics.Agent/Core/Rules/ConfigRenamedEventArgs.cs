using System;

namespace iPoint.ServiceStatistics.Agent.Core.Rules
{
    public class ConfigRenamedEventArgs : EventArgs
    {
        public string OldFileName { get; private set; }
        public string NewFileName { get; private set; }

        public ConfigRenamedEventArgs(string oldFileName, string newFileName)
        {
            OldFileName = oldFileName;
            NewFileName = newFileName;
        }
    }
}