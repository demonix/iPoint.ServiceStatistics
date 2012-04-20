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
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public Regex FileMask { get; private set; }
        public Encoding Encoding { get; private set; }

        public LogDescription(string configFileName, string fileMask, string encoding,
                              List<LogEventDescription> logEventDescriptions, string logDirectory)
        {
            FileMask = new Regex(fileMask, RegexOptions.Compiled);
            Encoding = Encoding.GetEncoding(encoding);
            ConfigFileName = configFileName;
            LogEventDescriptions = logEventDescriptions;
            LogDirectory = logDirectory;
        }

        private static List<LogEventDescription> GetLogEventDescrptions(string directoryName)
        {
            List<LogEventDescription> result = new List<LogEventDescription>();
            FileInfo[] fileInfos = new DirectoryInfo(directoryName).GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
            {
                result.Add(LogEventDescription.LoadFromConfigFile(fileInfo.FullName));
            }
            return result;
        }

        public static IEnumerable<LogDescription> ProcessLogDescription(string fileName)
        {
            _logger.Info("Processing LogDescription " + fileName);
            SettingsReader settingsReader = new SettingsReader(fileName);

            string fileMask = settingsReader.GetConfigParam("FileMask");
            string encoding = settingsReader.GetConfigParam("Encoding");
            List<string> possibleLogDirectories =
                settingsReader.GetConfigParams("LogDirectory").SelectMany(FilePathHelpers.FindDirectoriesOnFixedDisks).ToList();
            List<LogEventDescription> logEventDescriptions =
                GetLogEventDescrptions(Path.ChangeExtension(fileName, "EventDescripions"));
            _logger.Info("Total " + possibleLogDirectories.Count + " log directories: " +
                         String.Join("\r\n", possibleLogDirectories.ToArray()));

            foreach (string logDirectory in possibleLogDirectories)
            {
                foreach (string path in FilePathHelpers.GetDirectoriesByMaskedPath(logDirectory))
                {
                    yield return new LogDescription(fileName, fileMask, encoding, logEventDescriptions, path);
                }
            }
        }

        public string LogDirectory { get; private set; }
        public string ConfigFileName { get; set; }
        public List<LogEventDescription> LogEventDescriptions { get; private set; }
    }
}