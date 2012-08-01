using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;

namespace EventEvaluationLib
{
    public class EventEvaluatorRule
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        protected delegate string MatchDelegate(string logFileName, Match match);

        public string Id { get; private set; }
        public EventType EventType { get; private set; }
        public Regex RegexRule { get; private set; }
        public List<Regex> ExcludingRules { get; set; }
        public string DateFormat { get; private set; }

        private MatchDelegate _getCounterCategory;
        private MatchDelegate _getCounterName;
        private MatchDelegate _getDateTime;
        private MatchDelegate _getExtendedData;
        private MatchDelegate _getCounterInstance;
        private MatchDelegate _getSource;
        private MatchDelegate _getValue;



        public EventEvaluatorRule(EventType eventType, Regex regexRule, List<Regex> excludingRules, string eventSource, string counterCategory, string counterInstance, string counterName, string extendedData, string value, string dateTime, string dateFormat)
        {
            EventType = eventType;
            RegexRule = regexRule;
            HasExcludingRules = excludingRules.Count > 0;
            ExcludingRules = excludingRules;
            DateFormat = dateFormat;
            _getSource = CreateMatchDelegate(eventSource);
            _getCounterCategory = CreateMatchDelegate(counterCategory);
            _getCounterInstance = CreateMatchDelegate(counterInstance);
            _getCounterName = CreateMatchDelegate(counterName);
            _getExtendedData = CreateMatchDelegate(extendedData);
            _getValue = CreateMatchDelegate(value);
            _getDateTime = CreateMatchDelegate(dateTime);
        }

        public bool HasExcludingRules { get; private set; }

        public static EventEvaluatorRule CreateFromFile(string configFilePath)
        {
            try
            {
                SettingsReader settingsReader = new SettingsReader(configFilePath);
                EventType eventType = GetEventType(settingsReader.GetConfigParam("Type"));
                string eventSource = settingsReader.GetConfigParam("Source");
                string counterCategory = settingsReader.GetConfigParam("Category");
                string counterName = settingsReader.GetConfigParam("Counter");
                string counterInstance = settingsReader.GetConfigParam("Instance", false);
                string extendedData = settingsReader.GetConfigParam("ExtendedData", false);
                string value = settingsReader.GetConfigParam("Value");
                string dateTime = settingsReader.GetConfigParam("DateTime");
                string dateFormat = settingsReader.GetConfigParam("DateFormat");
                Regex regexRule = new Regex(settingsReader.GetConfigParam("Regex"), RegexOptions.Compiled);
                List<Regex> excludingRules =
                    settingsReader.GetConfigParams("ExcludeRegex",false).Distinct().Select(r => new Regex(r, RegexOptions.Compiled)).
                        ToList();

                var eventEvaluatorRule = new EventEvaluatorRule(eventType, regexRule, excludingRules, eventSource, counterCategory,
                                  counterInstance, counterName, extendedData, value,
                                  dateTime, dateFormat);
                _logger.Debug("Rule created from {0} is {2} and has {1} excludings",configFilePath, eventEvaluatorRule.ExcludingRules.Count, eventEvaluatorRule.RegexRule.ToString());
                return eventEvaluatorRule;
            }
            catch (Exception ex)
            {
                _logger.Error("Cannot load EventEvaluatorRule from " + configFilePath + ": " + ex);
                return null;
            }

        }

        private static EventType GetEventType(string logEventType)
        {
            switch (logEventType.ToLower())
            {
                case "performance":
                    return EventType.Performance;
                case "counter":
                    return EventType.Counter;
                default:
                    throw new Exception("Unknown log event type: " + logEventType);
            }
        }

       
        private MatchDelegate CreateMatchDelegate(string rule)
        {
            if (rule.ToLower() == "$host")
                return (logFileName, match) => Environment.MachineName;
            if (rule.ToLower() == "$datetime")
                return (logFileName, match) => DateTime.Now.ToString(CultureInfo.InvariantCulture);
            int elemenNumgerInPath;
            if (rule.ToLower().StartsWith("$path:") && Int32.TryParse(rule.Split(':').ElementAtOrDefault(1), out elemenNumgerInPath))
                return
                    (logFileName, match) => logFileName.Split('/', '\\').ElementAtOrDefault(elemenNumgerInPath);
            if (rule.ToLower() == "$rate")
            {
                if (RegexRule.GroupNumberFromName("timespan") > 0 && RegexRule.GroupNumberFromName("value") > 0)
                    return
                        (logFileName, match) =>
                            {
                                double value;
                                TimeSpan timeSpan;
                                if (Double.TryParse(match.Groups["value"].Value, out value))
                                    if (TimeSpan.TryParse(match.Groups["timespan"].Value, out timeSpan))
                                        if (timeSpan != TimeSpan.Zero)
                                            return (value/timeSpan.TotalMilliseconds).ToString("F");
                                return "0";
                            };
            }
            if (rule.ToLower().StartsWith("$replace:"))
                return (logFileName, match) => match.Result(rule.Split(new[] {':'}, 2)[1]);

            string regexGroupName = rule.Trim(new[] {'{', '}'});
            if (rule.StartsWith("{") && rule.EndsWith("}") && RegexRule.GroupNumberFromName(regexGroupName) >= 0)
                return (logFileName, match) => match.Groups[regexGroupName].Value;
            return (logFileName, match) => rule;
        }


        public string GetSource(string logFileName, Match match)
        {
            return _getSource(logFileName, match);
        }

        public string GetCounterCategory(string logFileName, Match match)
        {
            return _getCounterCategory(logFileName, match);
        }

        public string GetCounterName(string logFileName, Match match)
        {
            return _getCounterName(logFileName, match);
        }

        public string GetInstance(string logFileName, Match match)
        {
            return _getCounterInstance(logFileName, match);
        }

        public string GetValue(string logFileName, Match match)
        {
            return _getValue(logFileName, match);
        }

        public DateTime GetDateTime(string logFileName, Match match)
        {
            return DateTime.ParseExact(_getDateTime(logFileName, match), DateFormat, CultureInfo.InvariantCulture);
        }

        public string GetExtendedData(string logFileName, Match match)
        {
            return _getExtendedData(logFileName, match);
        }

    }
}