using System;
using System.Collections.Generic;

namespace Aggregation.Experimental.AggregationOperations
{
    public class MaxAggregationOperation : IAggregationOperation
    {
        public AggregationOperationResult Do(IList<UniversalValue> input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            AggregationOperationResult result = new AggregationOperationResult();
            result.Add("max", input.Max());
            return result;
        }
    }
}