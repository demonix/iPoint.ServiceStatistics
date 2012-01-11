using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using iPoint.ServiceStatistics.Agent.Core.LogFiles;

namespace iPoint.ServiceStatistics.Agent.Core.LogEvents
{
    public class LogDescription
    {
        public Regex FileMask { get; private set; }
        public Encoding Encoding { get; private set; }

        public LogDescription(string configFileName, string fileMask, string encoding, List<LogEventDescription> logEventDescriptions, string logDirectory)
        {
            FileMask = new Regex(fileMask,RegexOptions.Compiled);
            Encoding = Encoding.GetEncoding(encoding);
            ConfigFileName = configFileName;
            LogEventDescriptions = logEventDescriptions;
            LogDirectory = logDirectory;
        }

        public string LogDirectory { get; private set; }
        public string ConfigFileName { get; set; }
        public List<LogEventDescription> LogEventDescriptions { get; private set; }
    }
}