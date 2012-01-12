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
        public string ExtendedData;
        public string Value;
        
        public override string ToString()
        {
            return string.Format("Type: {0}, DateTime: {1}, Source: {2}, Category: {3}, Counter: {4}, Instance: {5}, ExtendedData: {6}, Value: {7}", Type, DateTime, Source, Category, Counter, Instance, ExtendedData, Value);
        }
    }
}