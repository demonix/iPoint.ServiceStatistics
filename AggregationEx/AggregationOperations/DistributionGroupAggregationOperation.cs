using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AggregationEx.AggregationOperations
{
    public class DistributionGroupAggregationOperation: IAggregationOperation
    {
        private class DistributionGroup
        {
            public DistributionGroup(double lowerBound, double upperBound)
            {
                LowerBound = lowerBound;
                UpperBound = upperBound;
            }

            public string AsKey
            {
                get
                {
                    string lowerStr = Double.IsNegativeInfinity(LowerBound) ? "-Inf" : LowerBound.ToString(CultureInfo.InvariantCulture);
                    string upperStr = Double.IsPositiveInfinity(UpperBound) ? "Inf" : UpperBound.ToString(CultureInfo.InvariantCulture);
                    return lowerStr + "/" + upperStr;
                }
            }

            /*public string AsHumanReadable
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
            }*/

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

            public double LowerBound { get; private set; }
            public double UpperBound { get; private set; }
        }

        private List<DistributionGroup> _distributionGroups;
        private double _valuesDataType;

        public DistributionGroupAggregationOperation(string parameters)
        {
            var boundaries = parameters.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries).Select(Double.Parse).ToList();
            if (boundaries.Count == 0)
                throw new Exception("boundaries list is empty");
            List<DistributionGroup> distributionGroups = new List<DistributionGroup>();
            double minBound, maxBound;

            minBound = Double.NegativeInfinity;
            maxBound = Double.PositiveInfinity;
            distributionGroups.Add(new DistributionGroup(minBound, boundaries.First()));
            for (int i = 0; i < boundaries.Count - 1; i++)
            {
                distributionGroups.Add(new DistributionGroup(boundaries[i], boundaries[i + 1]));
            }
            distributionGroups.Add(new DistributionGroup(boundaries.Last(), maxBound));
            _distributionGroups = distributionGroups;
        }

        public AggregationOperationResult Do(IList<double> input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            if (!input.Any())
                throw new InvalidOperationException("No elements to aggregate");
            double first = input.First();
            AggregationOperationResult result = new AggregationOperationResult(AggregationType.ValueDistributionGroups);
            int totalCount = input.Count;
            _distributionGroups.ForEach(
                dg =>
                result.Add(dg.AsKey,
                           (input.Count(i => i >= dg.LowerBound && i < dg.UpperBound)/totalCount)*100));
            return result;
        }
    }
}