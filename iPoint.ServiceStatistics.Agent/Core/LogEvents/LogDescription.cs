using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NLog;
using iPoint.ServiceStatistics.Agent.Core.LogFiles;

namespace iPoint.ServiceStatistics.Agent.Core.LogEvents
{
    public class LogDescription
    {
        public bool Equals(LogDescription other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Id, Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (LogDescription)) return false;
            return Equals((LogDescription) obj);
        }

        public override int GetHashCode()
        {
            return (Id != null ? Id.GetHashCode() : 0);
        }

        public static bool operator ==(LogDescription left, LogDescription right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LogDescription left, LogDescription right)
        {
            return !Equals(left, right);
        }

        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public Regex FileMask { get; private set; }
        public Encoding Encoding { get; private set; }
        public LogEventEvaluator LogEventEvaluator { get; private set; }

        private FileSystemWatcher _logEventDescriptionsWatcher;
        private FileSystemWatcher _logEventDescriptionsFolderCreationWatcher;

        public LogDescription(string configFileName, string fileMask, string encoding,
                              List<LogEventEvaluationRule> logEventDescriptions, List<string> logDirectories)
        {
            LogEventEvaluator = new LogEventEvaluator(logEventDescriptions);
            Id = configFileName;
            FileMask = new Regex(fileMask, RegexOptions.Compiled);
            Encoding = Encoding.GetEncoding(encoding);
            ConfigFileName = configFileName;
            LogEventDescriptions = logEventDescriptions;
            LogDirectories = logDirectories;
            string eventDescriptionsFolder = Path.ChangeExtension(configFileName, "EventDescriptions");
            if (Directory.Exists(eventDescriptionsFolder))
            CreateEventDescriptionsWatcher();
            else
            {
                _logEventDescriptionsFolderCreationWatcher = new FileSystemWatcher(
                    Path.GetDirectoryName(configFileName),
                    Path.ChangeExtension(Path.GetFileName(configFileName), "EventDescriptions"));
                _logEventDescriptionsFolderCreationWatcher.Created += OnEventDescriptionsFolderCreated;
                _logEventDescriptionsFolderCreationWatcher.EnableRaisingEvents = true;
            }
        }

        private volatile bool _logEventDescriptionsFolderCreationWatcherFired;
        private void OnEventDescriptionsFolderCreated(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            if (_logEventDescriptionsFolderCreationWatcherFired)
                _logEventDescriptionsFolderCreationWatcherFired = true;
            CreateEventDescriptionsWatcher();
        }

        private void CreateEventDescriptionsWatcher ()
        {
            _logEventDescriptionsWatcher = new FileSystemWatcher(Path.ChangeExtension(ConfigFileName, "EventDescriptions"));
            _logEventDescriptionsWatcher.NotifyFilter = NotifyFilters.LastWrite;
            _logEventDescriptionsWatcher.Changed += LogEventDescriptionChanged;
            _logEventDescriptionsWatcher.Created += LogEventDescriptionCreated;
            _logEventDescriptionsWatcher.Deleted += LogEventDescriptionDeleted;
            _logEventDescriptionsWatcher.EnableRaisingEvents = true;
        }

        private void LogEventDescriptionDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("Deleted " +e.FullPath );
        }

        private void LogEventDescriptionCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("Created " + e.FullPath);
        }

        private void LogEventDescriptionChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("Changed " + e.FullPath);
        }

        private static List<LogEventEvaluationRule> GetLogEventEvaluationRules(string directoryName)
        {
            List<LogEventEvaluationRule> result = new List<LogEventEvaluationRule>();
            if (!Directory.Exists(directoryName)) return result;
            FileInfo[] fileInfos = new DirectoryInfo(directoryName).GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
            {
                result.Add(LogEventEvaluationRule.LoadFromConfigFile(fileInfo.FullName));
            }
            return result;
        }

        public static LogDescription ProcessLogDescription(string fileName)
        {
            _logger.Info("Processing LogDescription " + fileName);
            SettingsReader settingsReader = new SettingsReader(fileName);

            string fileMask = settingsReader.GetConfigParam("FileMask");
            string encoding = settingsReader.GetConfigParam("Encoding");

            IEnumerable<string> possibleLogDirectories = settingsReader.GetConfigParams("LogDirectory").SelectMany(FilePathHelpers.FindDirectoriesOnFixedDisks);
            List<string> directories = possibleLogDirectories.SelectMany(FilePathHelpers.GetDirectoriesByMaskedPath).Distinct().ToList();

            _logger.Info(String.Format("Total {0} log directories:\r\n{1}", directories.Count, String.Join("\r\n", directories.ToArray())));
            
            string eventDescriptionsDir = Path.ChangeExtension(fileName, "EventDescriptions");
            List<LogEventEvaluationRule> logEventDescriptions = GetLogEventEvaluationRules(eventDescriptionsDir);
            return new LogDescription(fileName, fileMask, encoding, logEventDescriptions, directories);
        }

        public string Id { get; private set; }
        public List<string> LogDirectories { get; private set; }
        public string ConfigFileName { get; set; }
        public List<LogEventEvaluationRule> LogEventDescriptions { get; private set; }
    }
}