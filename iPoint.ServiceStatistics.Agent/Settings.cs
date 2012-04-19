using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NLog;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;

namespace iPoint.ServiceStatistics.Agent
{
    public class Settings
    {
        public List<LogDescription> LogDescriptions;
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public Settings()
        {
            LogDescriptions = GetLogDescrptions();
        }

        private static List<LogDescription> GetLogDescrptions()
        {
            List<LogDescription> result = new List<LogDescription>();
            FileInfo[] fileInfos = new DirectoryInfo("./settings/LogDescriptions").GetFiles("*.logDescription");
            foreach (FileInfo fileInfo in fileInfos)
            {
                _logger.Info("Processing log description from " + fileInfo.FullName);
                result.AddRange(ProcessLogDescription(fileInfo.FullName));
            }
            return result;
        }

        private static string GetConfigParam(string[] config, string paramName)
        {
            string result = null;
            foreach (string line in config)
            {
                string key = line.Split(new []{'='},2)[0].Trim();
                if (key.ToLower() != paramName.ToLower()) continue;
                string value = line.Split(new[] { '=' }, 2)[1].Trim();

                if (!String.IsNullOrEmpty(result))
                    throw new Exception(paramName + " specified miltiple times in config");
                result = value;
            }
            if (result == null)
                throw new Exception(paramName + " not specified in config");
            return result;
        }

        private static List<string> GetConfigParams(string[] config, string paramName)
        {
            List<string> result = new List<string>();
            foreach (string line in config)
            {
                string key = line.Split('=')[0].Trim();
                if (key.ToLower() != paramName.ToLower()) continue;
                string value = line.Split('=')[1].Trim();
                if (!result.Contains(value))
                    result.Add(value);
                else
                    _logger.Warn("Parameter {0} has duplicates values: {1}", paramName, value);
            }
            if (result.Count == 0)
                throw new Exception(paramName + " not specified in config");
            return result;
        }


        private static IEnumerable<LogDescription> ProcessLogDescription(string fileName)
        {
            _logger.Info("Processing LogDescription "+ fileName);
            string[] data = File.ReadAllLines(fileName);
            string fileMask = GetConfigParam(data, "FileMask");
            string encoding = GetConfigParam(data, "Encoding");
            List<string> logDirectories = GetConfigParams(data, "LogDirectory").SelectMany(
                p => Path.GetPathRoot(p) == Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)
                         ? DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed).Select(d => d.Name.TrimEnd('\\') + p)
                         : new List<string>() {p}).ToList();

            List<LogEventDescription> logEventDescriptions =
                GetLogEventDescrptions(Path.ChangeExtension(fileName, "EventDescripions"));
            _logger.Info("Total " + logDirectories.Count + " log directories: " + String.Join("\r\n", logDirectories.ToArray()));

            foreach (string logDirectory in logDirectories)
            {
                if (Directory.Exists(logDirectory))
                {
                    _logger.Info("Using " + logDirectory + " for LogDescription");
                    yield return new LogDescription(fileName, fileMask, encoding, logEventDescriptions, logDirectory);
                }
                else if (logDirectory.Contains("*"))
                {
                    _logger.Info("Log directory has mask: " + logDirectory);
                    List<string> paths = GetMultiplePaths("", logDirectory);
                    foreach (string path in paths)
                    {
                        _logger.Info("Using " + path + " for LogDescription");
                        yield return new LogDescription(fileName, fileMask, encoding, logEventDescriptions, path);
                    }
                }
                else
                {
                    _logger.Info("Directory " + logDirectory + " not exists");
                }
            }
        }

        private static List<string> GetMultiplePaths(string begining, string pathTailwithMask)
        {
            List<string> result = new List<string>();
            string currentPath = begining;
            string[] possibleDirectories;
            string[] parts = pathTailwithMask.Split(new[] {'\\', '/'}, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                if (Directory.Exists(currentPath))
                    result.Add(currentPath);
                return result;
            }
            if (!parts[0].Contains("*"))
            {
                possibleDirectories = new string[1];
                possibleDirectories[0] = currentPath += parts[0];
            }
            else
            {
                possibleDirectories = Directory.GetDirectories(currentPath, parts[0]);
            }
            foreach (string possibleDirectory in possibleDirectories)
            {
                result.AddRange(GetMultiplePaths(possibleDirectory + Path.DirectorySeparatorChar,
                                                 String.Join(new string(Path.DirectorySeparatorChar, 1),
                                                             parts.Skip(1).ToArray())));
            }

            return result;

        }

        private static List<LogEventDescription> GetLogEventDescrptions(string directoryName)
        {
            List<LogEventDescription> result = new List<LogEventDescription>();
            FileInfo[] fileInfos = new DirectoryInfo(directoryName).GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
            {
                result.Add(ProcessLogEventDescription(fileInfo.FullName));
            }
            return result;
        }

        private static LogEventDescription ProcessLogEventDescription(string fileName)
        {
            string[] data = File.ReadAllLines(fileName);
            string type = GetConfigParam(data, "Type");
            string source = GetConfigParam(data, "Source");
            string category = GetConfigParam(data, "Category");
            string counter = GetConfigParam(data, "Counter");
            string instance = GetConfigParam(data, "Instance");
            string extendedData = GetConfigParam(data, "ExtendedData");
            string value = GetConfigParam(data, "Value");
            string dateTime = GetConfigParam(data, "DateTime");
            string dateFormat = GetConfigParam(data, "DateFormat");
            string regex = GetConfigParam(data, "Regex");
            LogEventDescription result = new LogEventDescription(regex, type, source, category, counter, instance,
                                                                 extendedData, value, dateTime, dateFormat);
            return result;

        }
    }
}