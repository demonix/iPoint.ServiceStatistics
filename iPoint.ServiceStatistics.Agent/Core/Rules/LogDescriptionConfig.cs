using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NLog;

namespace iPoint.ServiceStatistics.Agent.Core.Rules
{
    public class LogDescriptionConfig: ChangeMonitorableConfigFile, IDisposable
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public Regex FileMask { get; private set; }
        public Encoding Encoding { get; private set; }
        public string Id { get; private set; }
        public List<string> LogDirectories { get; private set; }
        Dictionary<string, LogWatcher>  _logWatchers = new Dictionary<string, LogWatcher>();
      
        private EvaluatorRuleManager _ruleManager;
        

        public LogDescriptionConfig(string configFileName):base(configFileName)
        {
            Id = ConfigFileName;
            SettingsReader settingsReader = new SettingsReader(ConfigFileName);
            FileMask = new Regex(settingsReader.GetConfigParam("FileMask"), RegexOptions.Compiled);
            Encoding =  Encoding.GetEncoding(settingsReader.GetConfigParam("Encoding"));
            IEnumerable<string> possibleLogDirectories = settingsReader.GetConfigParams("LogDirectory").SelectMany(FilePathHelpers.FindDirectoriesOnFixedDisks);
            List<string> directories = possibleLogDirectories.SelectMany(FilePathHelpers.GetDirectoriesByMaskedPath).Distinct().ToList();
            LogDirectories = directories;
            _ruleManager = new EvaluatorRuleManager(Path.ChangeExtension(configFileName, "EventDescriptions"));
        }

       
        public new void Dispose()
        {
            base.Dispose();
            _ruleManager.Dispose();
        }

        #region equality members
        public override bool Equals(ChangeMonitorableConfigFile other)
        {
            LogDescriptionConfig o = other as LogDescriptionConfig;
            if (other == null) return false;
            return this.Equals(o);
        }

        public bool Equals(LogDescriptionConfig other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.FileMask.ToString(), FileMask.ToString()) &&
                   Equals(other.Encoding, Encoding) &&
                   Equals(other.Id, Id) &&
                   Equals(other.ConfigFileName, ConfigFileName) &&
                   other.LogDirectories.Count == LogDirectories.Count &&
                   other.LogDirectories.All(dir => LogDirectories.Contains(dir));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(LogDescriptionConfig)) return false;
            return Equals((LogDescriptionConfig)obj);
        }
      

        #endregion 
    }
}