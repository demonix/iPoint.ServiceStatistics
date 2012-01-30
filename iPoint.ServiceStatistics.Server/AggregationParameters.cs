using System;

namespace iPoint.ServiceStatistics.Server
{
    public class AggregationParameters
    {
        public AggregationParameters(string counterCategory, string counterName, AggregationType counterAggregationType, Type counterType)
        {
            CounterCategory = counterCategory;
            CounterName = counterName;
            CounterAggregationType = counterAggregationType;
            CounterType = counterType;
        }

        public string CounterCategory { get; private set; }
        public string CounterName { get; private set; }
        public AggregationType CounterAggregationType { get; private set; }
        public Type CounterType { get; private set; }
             
    }
}