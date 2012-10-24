using System;
using System.Collections.Generic;
using System.Linq;

namespace Aggregation.Experimental.AggregationOperations
{
    public class AggregationOperationResult
    {
        public AggregationOperationResult()
        {
            Value = new List<KeyValuePair<string, UniversalValue>>();
        }

        public List<KeyValuePair<string, UniversalValue>> Value { get; private set; }

        public AggregationOperationResult(IEnumerable<Tuple<string, UniversalValue>> list):this()
        {
            foreach (Tuple<string, UniversalValue> tuple in list)
            {
                Value.Add(new KeyValuePair<string, UniversalValue>(tuple.Item1, tuple.Item2));
            }
        }

        public void Add(string key, UniversalValue value)
        {
            Value.Add(new KeyValuePair<string, UniversalValue>(key, value));
        }

        public override string ToString()
        {
            return string.Format("Value: {0}", String.Join(", ",Value.Select(kvp=> kvp.Key+": "+kvp.Value)));
        }
    }
}