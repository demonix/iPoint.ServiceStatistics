using System;
using System.Collections.Generic;
using System.Linq;

namespace AggregationEx.AggregationOperations
{
    public class AggregationOperationResult
    {
        public AggregationOperationResult(AggregationType aggregationType)
        {
            AggregationType = aggregationType;
            Value = new List<KeyValuePair<string, double>>();
        }

        public AggregationOperationResult(AggregationType aggregationType, double value)
        {
            AggregationType = aggregationType;
            Value = new List<KeyValuePair<string, double>>();
            Value.Add(new KeyValuePair<string, double>(aggregationType.GetName(),value));
        }

        public AggregationType AggregationType { get; private set; }
        public List<KeyValuePair<string, double>> Value { get; private set; }

        public AggregationOperationResult(AggregationType aggregationType, IEnumerable<Tuple<string, double>> list)
            : this(aggregationType)
        {
            foreach (Tuple<string, double> tuple in list)
            {
                Value.Add(new KeyValuePair<string, double>(tuple.Item1, tuple.Item2));
            }
        }

        public void Add(string key, double value)
        {
            Value.Add(new KeyValuePair<string, double>(key, value));
        }

        public override string ToString()
        {
            return string.Format("Value: {0}", String.Join(", ",Value.Select(kvp=> kvp.Key+": "+kvp.Value)));
        }
    }
}