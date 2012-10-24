using System;
using System.Collections.Generic;

namespace Aggregation.Experimental.AggregationOperations
{
    public class AvgAggregationOperation : IAggregationOperation
    {
        public AggregationOperationResult Do(IList<UniversalValue> input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            AggregationOperationResult result = new AggregationOperationResult();
            result.Add("avg", input.Average());
            return result;
        }
    }
}