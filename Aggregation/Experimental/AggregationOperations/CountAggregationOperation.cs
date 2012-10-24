using System;
using System.Collections.Generic;
using System.Threading;

namespace Aggregation.Experimental.AggregationOperations
{
    public class CountAggregationOperation : IAggregationOperation
    {
        public AggregationOperationResult Do(IList<UniversalValue> input)
        {
            Thread.Sleep(2000);
            if (input == null)
                throw new ArgumentNullException("input");
            AggregationOperationResult result = new AggregationOperationResult();
            result.Add("count", new UniversalValue(input.Count));
            
            return result;
        }
    }
}