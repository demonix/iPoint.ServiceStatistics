using System;
using System.Collections.Generic;
using System.Linq;

namespace AggregationEx.AggregationOperations
{
    public class SumAggregationOperation:IAggregationOperation
    {
        public AggregationOperationResult Do(IList<double> input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            if (!input.Any())
                throw new InvalidOperationException("No elements to aggregate");
            return new AggregationOperationResult(AggregationType.Sum, input.Sum());
        }
    }
}