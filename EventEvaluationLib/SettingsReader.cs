using System;
using System.Collections.Generic;
using System.IO;
using NLog;

namespace EventEvaluationLib
{
    public class SettingsReader
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private Dictionary<string, List<string>> _settings = new Dictionary<string, List<string>>();

        private string _fileName;
        public SettingsReader(string fileName)
        {
            _fileName = fileName;
            string[] config = File.ReadAllLines(fileName);
            foreach (string line in config)
            {
                string key = line.Split('=')[0].Trim();
                if (key.StartsWith("#") || key.Length == 0) continue;
                string loweredKey = key.ToLower();
                string value = line.Split(new[] { '=' }, 2)[1].Trim();

                if (!_settings.ContainsKey(loweredKey))
                    _settings.Add(loweredKey, new List<string>());

                if (!_settings[loweredKey].Contains(value))
                    _settings[loweredKey].Add(value);
                else
                {
                    _logger.Warn(String.Format("Parameter {0} in config {2} has duplicates values: {1}", key, value, _fileName));
                }
            }
        }

        public string GetConfigParam(string paramName, bool required = true, string defaultValue = "")
        {

            if (!_settings.ContainsKey(paramName.ToLower()) && required)
                throw new Exception(String.Format("{0} not specified in config {1}", paramName, _fileName));
            if (!_settings.ContainsKey(paramName.ToLower()))
                return defaultValue;
            if (_settings[paramName.ToLower()].Count > 1)
                throw new Exception(String.Format("{0} specified miltiple times in config {1}", paramName, _fileName));
            return _settings[paramName.ToLower()][0];
        }

        public List<string> GetConfigParams(string paramName, bool required = true)
        {
            if (!_settings.ContainsKey(paramName.ToLower()) && required)
                throw new Exception(String.Format("{0} not specified in config {1}", paramName, _fileName));
            if (!_settings.ContainsKey(paramName.ToLower()))
                return new List<string> ();
            return _settings[paramName.ToLower()];
        }

    }
}