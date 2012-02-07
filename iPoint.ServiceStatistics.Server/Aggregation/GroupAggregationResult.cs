using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;

namespace iPoint.ServiceStatistics.Server.Aggregation
{
    public class GroupAggregationResult
    {
        public GroupAggregationResult(IGrouping<CounterGroup, UniversalValue> s, IEnumerable<Tuple<string, UniversalValue>> result)
        {
            CounterGroup = s.Key;
            Result = result;
        }

        public CounterGroup CounterGroup { get; private set; }
        
        public string StorageKey {
            get
            {
                return String.Format("{0}|{1}|{2}",
                                     CounterGroup.Source.Replace('.', '_'),
                                     CounterGroup.Instance.Replace('.', '_'),
                                     CounterGroup.ExtendedData.Replace('.', '_'));
                
            }
        }
        
        public IEnumerable<Tuple<string, UniversalValue>> Result { get; private set; }
      

        public override string ToString()
        {
            return String.Format("{{source: \"{0}\", instance: \"{1}\", extendedData: \"{2}\" value: {{{3}}} }}",
                                 CounterGroup.Source, CounterGroup.Instance, CounterGroup.ExtendedData, FormatResult());
        }
        string FormatResult()
        {
            return String.Join(", ", Result.Select(r => String.Format("{0}: \"{1}\"", r.Item1, r.Item2)));
        }


        public GroupAggregationResult(IGrouping<CounterGroup, UniversalValue> s, UniversalValue result)
        {
            CounterGroup = s.Key;
            Result = new List<Tuple<string, UniversalValue>>() { new Tuple<string, UniversalValue>("value", result) };
        }

       
    }
}
      
    
