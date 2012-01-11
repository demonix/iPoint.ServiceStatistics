using System;
using System.Text.RegularExpressions;

namespace iPoint.ServiceStatistics.Agent.Core.LogEvents
{
    public class TestLogEventDescription : LogEventDescription
    {
        private const string _regEx =
            @"^(?<date>\d{4}-\d{1,2}-\d{1,2} \d{1,2}:\d{1,2}:\d{1,2}(?>(?>,|\.)\d+)?).+INFO.*?\s+STAT\s+END\s+GET\s+(?<instance>.*?/mrreports)\s+(?<value>[\d.:,]+)\s+LIVE";

        private const LogEventType _eventType = LogEventType.Performance;
        private const string _source = "$host";
        private const string _category = "FT";
        private const string _instance = "{instance}";
        private const string _counter = "IB_Index_MR_Reports";
        private const string _value = "{value}";
        private const string _dateTime = "{date}";
        private const string _dateFormat = "yyyy-MM-dd HH:mm:ss,fff";

        public TestLogEventDescription()
            : base(_regEx,_eventType.ToString(),_source,_category,_counter,_instance,_value,_dateTime,_dateFormat)
        {

        }
    }


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
        private string _dateTimeRule;


        protected delegate string MatchDelegate(Match match);
        private MatchDelegate _getCategory;
        private MatchDelegate _getInstance;
        private MatchDelegate _getCounter;
        private MatchDelegate _getSource;
        private MatchDelegate _getValue;
        private MatchDelegate _getDateTime;
        
        private MatchDelegate CreateMatchDelegate(string rule)
        {
            if (rule.ToLower() == "$host")
                return match => Environment.MachineName;
            if (rule.ToLower() == "$datetime")
                return match => DateTime.Now.ToString();
            if (rule.ToLower() == "$rate")
            {
                if (Rule.GroupNumberFromName("timespan") > 0 && Rule.GroupNumberFromName("value") > 0)
                    return
                        match =>
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
                return match => match.Groups[regexGroupName].Value;
            return match => rule;
        }
        
        public LogEventDescription(string regexRule, string logEventType, string sourceRule, string categoryRule, string counterRule, string instanceRule, string valueRule, string dateTimeRule, string dateFormat)
        {
            Rule = new Regex(regexRule, RegexOptions.Compiled);
            DateFormat = dateFormat;
            EventType = GetLogEventType(logEventType);
            _sourceRule = sourceRule;
            _dateTimeRule = dateTimeRule;
            _valueRule = valueRule;
            _categoryRule = categoryRule;
            _instanceRule = instanceRule;
            _counterRule = counterRule;
            _getSource = CreateMatchDelegate(_sourceRule);
            _getCategory = CreateMatchDelegate(_categoryRule);
            _getInstance = CreateMatchDelegate(_instanceRule);
            _getCounter = CreateMatchDelegate(_counterRule);
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

        public string GetSource(Match match)
        {
            return _getSource(match);
        }
        public string GetCategory(Match match)
        {
            return _getCategory(match);
        }
        public string GetCounter(Match match)
        {
            return _getCounter(match);
        }
        public string GetInstance(Match match)
        {
            return _getInstance(match);
        }
        public string GetValue(Match match)
        {
            return _getValue(match);
        }
        public string GetDateTime(Match match)
        {
            return _getDateTime(match);
        }
    }
}