using System;
using System.Collections.Generic;
using System.Linq;

namespace Aggregation.Experimental.AggregationOperations
{
    public class DistributionGroupAggregationOperation: IAggregationOperation
    {
        private class DistributionGroup
        {
            public DistributionGroup(UniversalValue lowerBound, UniversalValue upperBound)
            {
                LowerBound = lowerBound;
                UpperBound = upperBound;
            }

            public string AsHumanReadable
            {
                get
                {
                    string lowerStr = LowerBound.ToString(), upperStr =  UpperBound.ToString();
                    if (LowerBound.Type == UniversalValue.UniversalClassType.TimeSpan)
                        lowerStr = TimeSpanDescr(LowerBound.TimespanValue);
                    if (UpperBound.Type == UniversalValue.UniversalClassType.TimeSpan)
                        upperStr = TimeSpanDescr(UpperBound.TimespanValue);
                    return "From " + lowerStr + " to " + upperStr;
                }
            }

            private string TimeSpanDescr(TimeSpan value)
            {
                return value.TotalHours > 0
                           ? Math.Round(value.TotalHours, 2) + " hr"
                           : value.TotalMinutes > 0
                                 ? Math.Round(value.TotalMinutes, 2) + " min"
                                 : value.TotalSeconds > 0
                                       ? Math.Round(value.TotalSeconds, 3) + " sec"
                                       : value.TotalMilliseconds > 0.001
                                             ? Math.Round(value.TotalSeconds, 3) +
                                               " msec"
                                             : Math.Round(value.TotalSeconds, 3) +
                                               " microsec";
            }

            public UniversalValue LowerBound { get; private set; }
            public UniversalValue UpperBound { get; private set; }
        }

        private List<DistributionGroup> _distributionGroups;
        private UniversalValue.UniversalClassType _valuesDataType;

        public DistributionGroupAggregationOperation(Type valuesDataType, string parameters)
        {
            _valuesDataType = UniversalValue.UniversalClassTypeByType(valuesDataType);
            var boundaries = parameters.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries).Select(t => UniversalValue.ParseFromString(valuesDataType, t)).ToList();
            if (boundaries.Count == 0)
                throw new Exception("boundaries list is empty");
            List<DistributionGroup> distributionGroups = new List<DistributionGroup>();
            UniversalValue minBound, maxBound;
            switch (_valuesDataType)
            {
                case UniversalValue.UniversalClassType.Numeric:
                    minBound = new UniversalValue(Double.MinValue);
                    maxBound = new UniversalValue(Double.MaxValue);
                    break;
                case UniversalValue.UniversalClassType.TimeSpan:
                    minBound = new UniversalValue(TimeSpan.MinValue);
                    maxBound = new UniversalValue(TimeSpan.MaxValue);
                    break;
                default:
                    throw new Exception("Type " + _valuesDataType + " not supported for interval distribution groupping");
            }
            distributionGroups.Add(new DistributionGroup(minBound, boundaries.First()));
            for (int i = 0; i < boundaries.Count - 1; i++)
            {
                distributionGroups.Add(new DistributionGroup(boundaries[i], boundaries[i + 1]));
            }
            distributionGroups.Add(new DistributionGroup(boundaries.Last(), maxBound));
            _distributionGroups = distributionGroups;
        }

        public AggregationOperationResult Do(IList<UniversalValue> input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            UniversalValue first = input.FirstOrDefault();
            double totalCount = input.Count();
            if (first == null)
                throw new Exception("Sequence contains no elements");
            if (first.Type != _valuesDataType)
                throw new Exception("Input type not equals to group type");
            
            AggregationOperationResult result = new AggregationOperationResult();

            _distributionGroups.ForEach(
                dg =>
                result.Add(dg.AsHumanReadable,
                           new UniversalValue((input.Count(i => i >= dg.LowerBound && i < dg.UpperBound)/totalCount)*100)));

            return result;
        }
    }
}