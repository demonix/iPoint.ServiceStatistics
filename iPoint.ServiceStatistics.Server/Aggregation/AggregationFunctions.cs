using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace iPoint.ServiceStatistics.Server.Aggregation
{
    public class AggregationFunctions<T>
    {
        private IEnumerable<UniversalValue> list = new List<UniversalValue>();
        private IEnumerable<int> list2 = new List<int>();
        
      
    
        void Test()
        {
            list.Sum();
            list2.Sum();
            list.Max();
            list.Min();
            list.Percentile(new List<double>{45.00});

        }
    }
}