using System;
using System.Collections.Generic;

namespace Aggregation.Experimental.AggregationOperations
{
    public class MinAggregationOperation : IAggregationOperation
    {
        public AggregationOperationResult Do(IList<UniversalValue> input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            AggregationOperationResult result = new AggregationOperationResult();
            result.Add("min", input.Min());
            return result;
        }
    }
}