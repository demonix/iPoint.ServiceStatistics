using System;
using System.Collections.Generic;
using System.Linq;
using AggregationEx.AggregationOperations;

namespace AggregationEx
{
    public static class AggregatedValueExtensions
    {
        public static IEnumerable<AggregatedValue> Compact(this IEnumerable<AggregatedValue> source)
        {
            Dictionary<AggregationKey,List<AggregatedValue>> dic = new Dictionary<AggregationKey, List<AggregatedValue>>();
            foreach (var aggregatedValue in source)
            {
                var aggregationKey = new AggregationKey(aggregatedValue.Date, aggregatedValue.Props);
                if (!dic.ContainsKey(aggregationKey))
                    dic[aggregationKey] = new List<AggregatedValue>();
                dic[aggregationKey].Add(aggregatedValue);
            }
            foreach (var kvp in dic)
            {
                AggregatedValue result = new AggregatedValue(kvp.Key);
                if (kvp.Value.Any(v => v.Sum.HasValue))
                    result.AddResult(new AggregationOperationResult(AggregationType.Sum, kvp.Value.Where(v => v.Sum.HasValue).Sum(v => v.Sum.Value)));
                if (kvp.Value.Any(v => v.Count.HasValue))
                    result.AddResult(new AggregationOperationResult(AggregationType.Count, kvp.Value.Where(v => v.Count.HasValue).Sum(v => v.Count.Value)));
                if (kvp.Value.Any(v => v.Min.HasValue))
                    result.AddResult(new AggregationOperationResult(AggregationType.Min, kvp.Value.Where(v => v.Min.HasValue).Min(v => v.Min.Value)));
                if (kvp.Value.Any(v => v.Max.HasValue))
                    result.AddResult(new AggregationOperationResult(AggregationType.Max, kvp.Value.Where(v => v.Max.HasValue).Max(v => v.Max.Value)));
                if (kvp.Value.Any(v => v.Avg.HasValue))
                    result.AddResult(new AggregationOperationResult(AggregationType.Avg, kvp.Value.Where(v => v.Avg.HasValue).Average(v => v.Avg.Value)));
                if (result.Count!=0 && (result.Max == 0.0))
                    Console.WriteLine("!");

                yield return result;
            }
        }
    }
}