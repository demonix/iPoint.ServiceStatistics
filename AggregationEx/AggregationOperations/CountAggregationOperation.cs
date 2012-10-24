using System;
using System.Collections.Generic;

namespace AggregationEx.AggregationOperations
{
    public class CountAggregationOperation : IAggregationOperation
    {
        public AggregationOperationResult Do(IList<double> input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            return new AggregationOperationResult(AggregationType.Count, input.Count);
        }
    }
}