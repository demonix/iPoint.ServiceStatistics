using Aggregation.Experimental.AggregationOperations;

namespace Aggregation.Experimental
{
    public class AggregatedValue
    {
        public CounterGroup CounterGroup { get; private set; }
        public AggregationOperationResult Value { get; private set; }

        public AggregatedValue(CounterGroup counterGroup, AggregationOperationResult value)
        {
            CounterGroup = counterGroup;
            Value = value;
        }

        public override string ToString()
        {
            return string.Format("CounterGroup: {0}, Value: {1}", CounterGroup, Value);
        }
    }
}