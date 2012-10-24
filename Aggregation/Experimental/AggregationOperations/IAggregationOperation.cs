using System.Collections.Generic;

namespace Aggregation.Experimental.AggregationOperations
{
    public interface IAggregationOperation
    {
        AggregationOperationResult Do(IList<UniversalValue> input);
    }
}