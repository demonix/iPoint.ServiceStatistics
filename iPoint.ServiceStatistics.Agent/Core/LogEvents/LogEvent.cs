using System;

namespace iPoint.ServiceStatistics.Agent.Core.LogEvents
{
    [Serializable]
    public class LogEvent
    {
        public LogEventType Type;
        public DateTime DateTime;
        public string Source;
        public string Category;
        public string Counter;
        public string Instance;
        public EventData Data;
        
        public override string ToString()
        {
            return string.Format("Type: {0}, DateTime: {1}, Source: {2}, Category: {3}, Counter: {4}, Instance: {5}, Data: {6}", Type, DateTime, Source, Category, Counter, Instance, Data);
        }
    }

    [Serializable]
    public class EventData
    {
        public string ExtendedInfo { get; private set; }
        public string Value { get; private set; }

        public EventData(string extendedInfo, string value)
        {
            ExtendedInfo = extendedInfo;
            Value = value;

        }
    }
}