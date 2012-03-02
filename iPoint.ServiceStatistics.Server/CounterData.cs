using System;
using iPoint.ServiceStatistics.Server.Aggregation;

namespace iPoint.ServiceStatistics.Server
{
    public class CounterData
    {
        public DateTime DateTime { get; private set; }
        public double? Value { get; private set; }

        public CounterData(DateTime dateTime, double? value)
        {
            DateTime = dateTime;
            Value = value;
        }
    }
}