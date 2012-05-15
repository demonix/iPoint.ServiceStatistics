using System;
using System.Globalization;
using System.Text.RegularExpressions;
using NLog;

namespace iPoint.ServiceStatistics.Agent.Core.LogEvents
{
    public class LogEventEvaluationRule
    {
        private readonly Regex _rule;
        private MatchDelegate _getCounterCategory;
        private MatchDelegate _getCounterName;
        private MatchDelegate _getDateTime;
        private MatchDelegate _getExtendedData;
        private MatchDelegate _getInstance;
        private MatchDelegate _getSource;
        private MatchDelegate _getValue;
     

        public LogEventEvaluationRule(string fileName, string regexRule, string logEventType, string sourceEvaluationRule,
                                      string counterCategoryEvaluationRule, string counterNameEvaluationRule,
                                      string instanceEvaluationRule, string extendedDataEvaluationRule, string valueEvaluationRule,
                                      string dateTimeEvaluationRule, string dateFormat)
        {
            Id = fileName;
            _rule = new Regex(regexRule, RegexOptions.Compiled);
            DateFormat = dateFormat;
            EventType = GetLogEventType(logEventType);
            _getSource = CreateMatchDelegate(sourceEvaluationRule);
            _getCounterCategory = CreateMatchDelegate(counterCategoryEvaluationRule);
            _getInstance = CreateMatchDelegate(instanceEvaluationRule);
            _getCounterName = CreateMatchDelegate(counterNameEvaluationRule);
            _getExtendedData = CreateMatchDelegate(extendedDataEvaluationRule);
            _getValue = CreateMatchDelegate(valueEvaluationRule);
            _getDateTime = CreateMatchDelegate(dateTimeEvaluationRule);
        }

        public bool Equals(LogEventEvaluationRule other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Id, Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (LogEventEvaluationRule)) return false;
            return Equals((LogEventEvaluationRule) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(LogEventEvaluationRule left, LogEventEvaluationRule right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LogEventEvaluationRule left, LogEventEvaluationRule right)
        {
            return !Equals(left, right);
        }

        public LogEventType EventType { get; private set; }
        public string DateFormat { get; private set; }
        public string Id { get; private set; }

        public Match Match(string input)
        {
            return _rule.Match(input);
        }

        private MatchDelegate CreateMatchDelegate(string rule)
        {
            if (rule.ToLower() == "$host")
                return (logFileName, match) => Environment.MachineName;
            if (rule.ToLower() == "$datetime")
                return (logFileName, match) => DateTime.Now.ToString(CultureInfo.InvariantCulture);
            if (rule.ToLower().StartsWith("$path:"))
                return (logFileName, match) => logFileName.Split('/', '\\')[Convert.ToInt32(rule.Split(':')[1])];
            if (rule.ToLower() == "$rate")
            {
                if (_rule.GroupNumberFromName("timespan") > 0 && _rule.GroupNumberFromName("value") > 0)
                    return
                        (logFileName, match) =>
                            {
                                double value;
                                TimeSpan timeSpan;
                                if (Double.TryParse(match.Groups["value"].Value, out value))
                                    if (TimeSpan.TryParse(match.Groups["timespan"].Value, out timeSpan))
                                        return (value/timeSpan.Milliseconds).ToString("F");
                                return "0";
                            };
            }
            string regexGroupName = rule.Trim(new[] {'{', '}'});
            if (rule.StartsWith("{") && rule.EndsWith("}") && _rule.GroupNumberFromName(regexGroupName) >= 0)
                return (logFileName, match) => match.Groups[regexGroupName].Value;
            return (logFileName, match) => rule;
        }

        public static LogEventEvaluationRule LoadFromConfigFile(string fileName)
        {
            SettingsReader settingsReader = new SettingsReader(fileName);
            string type = settingsReader.GetConfigParam("Type");
            string source = settingsReader.GetConfigParam("Source");
            string counterCategory = settingsReader.GetConfigParam("Category");
            string counterName = settingsReader.GetConfigParam("Counter");
            string instance = settingsReader.GetConfigParam("Instance", false);
            string extendedData = settingsReader.GetConfigParam("ExtendedData", false);
            string value = settingsReader.GetConfigParam("Value");
            string dateTime = settingsReader.GetConfigParam("DateTime");
            string dateFormat = settingsReader.GetConfigParam("DateFormat");
            string regex = settingsReader.GetConfigParam("Regex");
            LogEventEvaluationRule result = new LogEventEvaluationRule(fileName, regex, type, source, counterCategory,
                                                                       counterName, instance,
                                                                       extendedData, value, dateTime, dateFormat);
            return result;
        }

        private LogEventType GetLogEventType(string logEventType)
        {
            switch (logEventType.ToLower())
            {
                case "performance":
                    return LogEventType.Performance;
                case "counter":
                    return LogEventType.Counter;
                default:
                    throw new Exception("Unknown log event type: " + logEventType);
            }
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
            return _getInstance(logFileName, match);
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

        #region Nested type: MatchDelegate

        protected delegate string MatchDelegate(string logFileName, Match match);

        #endregion
    }
}