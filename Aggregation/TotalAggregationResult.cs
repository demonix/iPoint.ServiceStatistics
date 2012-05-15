using System;
using System.Collections.Generic;
using System.Linq;

namespace Aggregation
{
    public class TotalAggregationResult
    {
        public readonly DateTime Date = DateTime.Now;
        public string CounterCategory { get; private set; }
        public string CounterName { get; private set; }
        public AggregationType CounterAggregationType { get; private set; }
        public IEnumerable<GroupAggregationResult> ResultGroups { get; private set; }
        public IEnumerable<string> AllSources { get; private set; }
        public IEnumerable<string> AllInstances { get; private set; }
        public IEnumerable<string> AllExtendedDatas { get; private set; }


        public TotalAggregationResult(string counterCategory, string counterName, AggregationType counterAggregationType, IEnumerable<GroupAggregationResult> groupAggregationResults)
        {
            CounterCategory = counterCategory;
            CounterName = counterName;
            CounterAggregationType = counterAggregationType;
            ResultGroups = groupAggregationResults;
            AllSources = groupAggregationResults.Select(g => g.CounterGroup.Source).Distinct().Where(g => g != "ALL_SOURCES");
            AllInstances = groupAggregationResults.Select(g => g.CounterGroup.Instance).Distinct().Where(g => g != "ALL_INSTANCES");
            AllExtendedDatas = groupAggregationResults.Select(g => g.CounterGroup.ExtendedData).Distinct().Where(g => g != "ALL_EXTDATA");
        }
       

        public override string ToString()
        {
            return string.Format("{{date: {0}, CounterCategory: \"{1}\", CounterName: \"{2}\", type: \"{3}\",  data: [{4}] }}", 
                                 Date, CounterCategory, CounterName, CounterAggregationType, String.Join(", ",ResultGroups));
        }

        
    }
    
}