using System;
using System.Globalization;

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
            return string.Format("Type: {0}, DateTime: {1}, Source: {2}, Category: {3}, Counter: {4}, Instance: {5}, ExtendedData: {6}, Value: {7}", Type, DateTime.ToLocalTime().ToString(CultureInfo.InvariantCulture), Source, Category, Counter, Instance, ExtendedData, Value);
        }
    }

    #region sandbox

    /*public class CounterLogEvent: ILogEvent<Int32>
    {
        public LogEventType Type { get; set; }

        public Type DataType { get; set ; }

        public DateTime DateTime { get; set; }
        public string Source { get; set; }
        public string Category { get; set; }
        public string Counter { get; set; }
        public string Instance { get; set; }
        public string ExtendedData { get; set; }
        public Int32 Value { get; set; }

        public override string ToString()
        {
            return string.Format("Type: {0}, DateTime: {1}, Source: {2}, Category: {3}, Counter: {4}, Instance: {5}, ExtendedData: {6}, Value: {7}", Type, DateTime, Source, Category, Counter, Instance, ExtendedData, Value);
        }
    }

    public interface ILogEvent<T> : ILogEventType
    {
        new LogEventType Type { get; set; }
        DateTime DateTime { get; set; }
        string Source { get; set; }
        string Category { get; set; }
        string Counter { get; set; }
        string Instance { get; set; }
        string ExtendedData { get; set; }
        T Value { get; set; }
    }
    public interface ILogEventType
    {
        LogEventType Type { get; set; }
        Type DataType { get; set; }
    }
    
    public class Test
    {
        void TestMain()
        {
            ILogEvent<Int32> a = new CounterLogEvent()
                                     {
                                         Category = "asd"
                                     };
            object b = a;

            ILogEventType d = (ILogEventType)b;

            var c = GetCounterName(d, default());


        }

        ILogEvent<T> GetCounterName<T>(ILogEventType cnt, T defaultValue)
        {
            
        }

    }*/
    #endregion sandbox
}