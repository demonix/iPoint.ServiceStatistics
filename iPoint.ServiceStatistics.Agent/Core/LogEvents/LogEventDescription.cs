using System;
using System.Text.RegularExpressions;

namespace iPoint.ServiceStatistics.Agent.Core.LogEvents
{
  
    public class LogEventDescription
    {
        public Regex Rule { get; private set; }
        public LogEventType EventType { get; private set; }
        public string DateFormat { get; private set; }
        private string _sourceRule;
        private string _categoryRule;
        private string _instanceRule;
        private string _counterRule;
        private string _valueRule;
        private string _extendedDataRule;
        private string _dateTimeRule;


        protected delegate string MatchDelegate(string logFileName, Match match);
        private MatchDelegate _getCategory;
        private MatchDelegate _getInstance;
        private MatchDelegate _getCounter;
        private MatchDelegate _getSource;
        private MatchDelegate _getValue;
        private MatchDelegate _getExtendedData;
        private MatchDelegate _getDateTime;
        
        private MatchDelegate CreateMatchDelegate(string rule)
        {
            if (rule.ToLower() == "$host")
                return (logFileName, match) => Environment.MachineName;
            if (rule.ToLower() == "$datetime")
                return (logFileName, match) => DateTime.Now.ToString();
            if (rule.ToLower().StartsWith("$path:"))
                return (logFileName, match) => logFileName.Split('/','\\')[Convert.ToInt32(rule.Split(':')[1])];
            if (rule.ToLower() == "$rate")
            {
                if (Rule.GroupNumberFromName("timespan") > 0 && Rule.GroupNumberFromName("value") > 0)
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
            string regexGroupName = rule.Trim(new[] { '{', '}' });
            if (rule.StartsWith("{") && rule.EndsWith("}") && Rule.GroupNumberFromName(regexGroupName) >= 0)
                return (logFileName, match) => match.Groups[regexGroupName].Value;
            return (logFileName, match) => rule;
        }
        
        public LogEventDescription(string regexRule, string logEventType, string sourceRule, string categoryRule, string counterRule, string instanceRule, string extendedDataRule, string valueRule, string dateTimeRule, string dateFormat)
        {
            Rule = new Regex(regexRule, RegexOptions.Compiled);
            DateFormat = dateFormat;
            EventType = GetLogEventType(logEventType);
            _sourceRule = sourceRule;
            _dateTimeRule = dateTimeRule;
            _valueRule = valueRule;
            _categoryRule = categoryRule;
            _instanceRule = instanceRule;
            _extendedDataRule = extendedDataRule;
            _counterRule = counterRule;
            _getSource = CreateMatchDelegate(_sourceRule);
            _getCategory = CreateMatchDelegate(_categoryRule);
            _getInstance = CreateMatchDelegate(_instanceRule);
            _getCounter = CreateMatchDelegate(_counterRule);
            _getExtendedData = CreateMatchDelegate(_extendedDataRule);
            _getValue = CreateMatchDelegate(_valueRule);
            _getDateTime = CreateMatchDelegate(_dateTimeRule);
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
        public string GetCategory(string logFileName, Match match)
        {
            return _getCategory(logFileName, match);
        }
        public string GetCounter(string logFileName, Match match)
        {
            return _getCounter(logFileName, match);
        }
        public string GetInstance(string logFileName, Match match)
        {
            return _getInstance(logFileName, match);
        }
        public string GetValue(string logFileName, Match match)
        {
            return _getValue(logFileName, match);
        }
        public string GetDateTime(string logFileName, Match match)
        {
            return _getDateTime(logFileName, match);
        }
        public string GetExtendedData(string logFileName, Match match)
        {
            return _getExtendedData(logFileName, match);
        }
    }
}