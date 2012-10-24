using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AggregationEx.AggregationOperations
{
    public class PercentileAggregationOperation: IAggregationOperation
    {
        private List<double> _percents;

        public PercentileAggregationOperation(string parameters)
        {
            var percents = parameters.Split(new [] {'|'}, StringSplitOptions.RemoveEmptyEntries).Select(Double.Parse).ToList();
            if (percents.Count == 0)
                throw new Exception("percentiles list is empty");
            _percents = percents;
        }

        private double Percentile(IOrderedEnumerable<double> sortedData, double p)
        {
            int count = sortedData.Count();
            if (count == 0) return 0;
            if (count == 1) return sortedData.Last();
            if (p >= 100.0d) return sortedData.Last();

            double position = (count + 1) * p / 100d;
            double leftNumber, rightNumber;

            double n = p / 100d * (count - 1) + 1d;

            if (position >= 1)
            {
                leftNumber = sortedData.ElementAt((int)Math.Floor(n) - 1);
                rightNumber = sortedData.ElementAt((int)Math.Floor(n));
            }
            else
            {
                leftNumber = sortedData.First();
                rightNumber = sortedData.ElementAt(1);
            }

            if (Math.Abs(leftNumber - rightNumber) < Double.Epsilon)
                return leftNumber;
            else
            {
                double part = n - Math.Floor(n);
                return leftNumber + part*(rightNumber - leftNumber);
            }
        }

        public AggregationOperationResult Do(IList<double> input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            if (!input.Any())
                throw new InvalidOperationException("No elements to aggregate");
            double first = input.First();
            
            AggregationOperationResult result = new AggregationOperationResult(AggregationType.Percentile);
            IOrderedEnumerable<double> sortedData = input.OrderBy(i => i);
            _percents.ForEach(pp => result.Add(pp.ToString(CultureInfo.InvariantCulture), Percentile(sortedData, pp)));
            return result;
        }
    }
}