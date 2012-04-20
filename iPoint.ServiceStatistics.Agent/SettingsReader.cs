﻿using System;
using System.Collections.Generic;
using System.IO;
using NLog;

namespace iPoint.ServiceStatistics.Agent
{
    public class SettingsReader
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private Dictionary<string, List<string>> _settings= new Dictionary<string, List<string>>();
        
        public SettingsReader(string fileName)
        {
            string[] config = File.ReadAllLines(fileName);
            foreach (string line in config)
            {
                string key = line.Split('=')[0].Trim();
                string loweredKey = key.ToLower();
                string value = line.Split(new[] {'='}, 2)[1].Trim();

                if (!_settings.ContainsKey(loweredKey))
                    _settings.Add(loweredKey, new List<string>());

                if (!_settings[loweredKey].Contains(value))
                    _settings[loweredKey].Add(value);
                else
                {
                    _logger.Warn("Parameter {0} has duplicates values: {1}", key, value);
                }
            }
        }

        public string GetConfigParam(string paramName, bool required = true)
        {

            if (!_settings.ContainsKey(paramName.ToLower()) && required)
                throw new Exception(paramName + " not specified in config");
            if (!_settings.ContainsKey(paramName.ToLower()))
                return "";
            if (_settings[paramName.ToLower()].Count > 1)
                throw new Exception(paramName + " specified miltiple times in config");
            return _settings[paramName.ToLower()][0];
        }

        public List<string> GetConfigParams(string paramName, bool required = true)
        {
            if (!_settings.ContainsKey(paramName.ToLower()) && required)
                throw new Exception(paramName + " not specified in config");
            if (!_settings.ContainsKey(paramName.ToLower()))
                return new List<string>{""};
            return _settings[paramName.ToLower()];
        }

    }
}