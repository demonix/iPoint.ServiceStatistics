using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using NLog;

namespace iPoint.ServiceStatistics.Agent.Core.Rules
{
    public class EvaluatorRuleConfig: ChangeMonitorableConfigFile, IDisposable
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();


        public new void Dispose()
        {
            base.Dispose();
        }

        

        public LogEventType EventType { get; private set; }
        public string EventSource { get; private set; }
        public string CounterCategory { get; private set; }
        public string CounterName { get; private set; }
        public string CounterInstance { get; private set; }
        public string ExtendedData { get; private set; }
        public string Value { get; private set; }
        public string DateTime { get; private set; }
        public string DateFormat { get; private set; }
        public Regex Regex { get; private set; }

        public EvaluatorRuleConfig(string configFileName): base(configFileName)
        {
            SettingsReader settingsReader = new SettingsReader(ConfigFileName);
            EventType = GetLogEventType(settingsReader.GetConfigParam("Type"));
            EventSource = settingsReader.GetConfigParam("Source");
            CounterCategory = settingsReader.GetConfigParam("Category");
            CounterName = settingsReader.GetConfigParam("Counter");
            CounterInstance = settingsReader.GetConfigParam("Instance", false);
            ExtendedData = settingsReader.GetConfigParam("ExtendedData", false);
            Value = settingsReader.GetConfigParam("Value");
            DateTime = settingsReader.GetConfigParam("DateTime");
            DateFormat = settingsReader.GetConfigParam("DateFormat");
            Regex = new Regex(settingsReader.GetConfigParam("Regex"),RegexOptions.Compiled);
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
        #region equality members
        public override bool Equals(ChangeMonitorableConfigFile other)
        {
            EvaluatorRuleConfig o = other as EvaluatorRuleConfig;
            if (other == null )return false;
            return this.Equals(o);
        }

        public bool Equals(EvaluatorRuleConfig other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.ConfigFileName, ConfigFileName) && Equals(other.EventType, EventType) &&
                Equals(other.EventSource, EventSource) && Equals(other.CounterCategory, CounterCategory) &&
                Equals(other.CounterName, CounterName) && Equals(other.CounterInstance, CounterInstance) &&
                Equals(other.ExtendedData, ExtendedData) && Equals(other.Value, Value) &&
                Equals(other.DateTime, DateTime) && Equals(other.DateFormat, DateFormat) &&
                Equals(other.Regex.ToString(), Regex.ToString());
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(EvaluatorRuleConfig)) return false;
            return Equals((EvaluatorRuleConfig)obj);
        }

   
        #endregion
    }
}