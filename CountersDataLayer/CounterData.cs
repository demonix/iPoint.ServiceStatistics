using System;
using Aggregation;

namespace CountersDataLayer
{
    public class CounterData
    {
        public DateTime DateTime { get; private set; }
        public UniversalValue Value { get; private set; }

        public CounterData(DateTime dateTime, UniversalValue value)
        {
            DateTime = dateTime;
            Value = value;
        }
    }
}