using System.Collections.Generic;

namespace AggregationEx.AggregationOperations
{
    public interface IAggregationOperation
    {
        AggregationOperationResult Do(IList<double> input);
    }
}