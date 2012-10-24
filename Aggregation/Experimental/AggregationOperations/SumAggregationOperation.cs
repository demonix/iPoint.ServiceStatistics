using System;
using System.Collections.Generic;

namespace Aggregation.Experimental.AggregationOperations
{
    public class SumAggregationOperation:IAggregationOperation
    {
        public AggregationOperationResult Do(IList<UniversalValue> input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            AggregationOperationResult result = new AggregationOperationResult();
            result.Add("sum", input.Sum());
            return result;
        }
    }
}