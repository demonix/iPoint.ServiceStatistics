using System;

namespace AggregationEx.AggregationOperations
{
    internal class AggregationOperationFactory
    {
        public static IAggregationOperation Create(string aggregationType, string parameters)
        {
            AggregationType at;
            if (!Enum.TryParse(aggregationType, true, out at))
                throw new Exception("Unsupported aggregation type: " + aggregationType);
            switch (at)
            {
                case AggregationType.Percentile:
                    return new PercentileAggregationOperation(parameters);
                case AggregationType.ValueDistributionGroups:
                    return new DistributionGroupAggregationOperation(parameters);
                case AggregationType.Sum:
                    return new SumAggregationOperation();
                case AggregationType.Count:
                    return new CountAggregationOperation();
                case AggregationType.Min:
                    return new MinAggregationOperation();
                case AggregationType.Max:
                    return new MaxAggregationOperation();
                case AggregationType.Avg:
                    return new AvgAggregationOperation();
                default:
                    throw new Exception("Unsupported aggregation type: " + at);
            }
        }
    }
}