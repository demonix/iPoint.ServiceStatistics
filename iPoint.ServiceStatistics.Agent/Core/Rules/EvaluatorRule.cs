using System;
using System.Globalization;
using System.Text.RegularExpressions;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;

namespace iPoint.ServiceStatistics.Agent.Core.Rules
{
    public class EvaluatorRule:IDisposable
    {
        protected delegate string MatchDelegate(string logFileName, Match match);

        public string Id { get; private set; }
        public readonly EvaluatorRuleConfig Config;
        
        public void Dispose()
        {
            Config.Dispose();
        }

        private MatchDelegate _getCounterCategory;
        private MatchDelegate _getCounterName;
        private MatchDelegate _getDateTime;
        private MatchDelegate _getExtendedData;
        private MatchDelegate _getCounterInstance;
        private MatchDelegate _getSource;
        private MatchDelegate _getValue;

        public EvaluatorRule(EvaluatorRuleConfig config)
        {
            Config = config;
            Id = Config.ConfigFileName;
            _getSource = CreateMatchDelegate(Config.EventSource);
            _getCounterCategory = CreateMatchDelegate(Config.CounterCategory);
            _getCounterInstance = CreateMatchDelegate(Config.CounterInstance);
            _getCounterName = CreateMatchDelegate(Config.CounterName);
            _getExtendedData = CreateMatchDelegate(Config.ExtendedData);
            _getValue = CreateMatchDelegate(Config.Value);
            _getDateTime = CreateMatchDelegate(Config.DateTime);
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
                if (Config.Regex.GroupNumberFromName("timespan") > 0 && Config.Regex.GroupNumberFromName("value") > 0)
                    return
                        (logFileName, match) =>
                        {
                            double value;
                            TimeSpan timeSpan;
                            if (Double.TryParse(match.Groups["value"].Value, out value))
                                if (TimeSpan.TryParse(match.Groups["timespan"].Value, out timeSpan))
                                    return (value / timeSpan.Milliseconds).ToString("F");
                            return "0";
                        };
            }
            string regexGroupName = rule.Trim(new[] { '{', '}' });
            if (rule.StartsWith("{") && rule.EndsWith("}") && Config.Regex.GroupNumberFromName(regexGroupName) >= 0)
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
            return DateTime.ParseExact(_getDateTime(logFileName, match), Config.DateFormat, CultureInfo.InvariantCulture);
        }

        public string GetExtendedData(string logFileName, Match match)
        {
            return _getExtendedData(logFileName, match);
        }

    }
}