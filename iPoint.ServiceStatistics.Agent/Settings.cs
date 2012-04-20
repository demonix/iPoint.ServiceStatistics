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
                result.AddRange(LogDescription.ProcessLogDescription(fileInfo.FullName));
            }
            return result;
        }
    }
}