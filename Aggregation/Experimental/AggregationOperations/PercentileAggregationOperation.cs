using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Aggregation.Experimental.AggregationOperations
{
    public class PercentileAggregationOperation: IAggregationOperation
    {
        private List<double> _percents;
        private UniversalValue.UniversalClassType _valuesDataType;

        public PercentileAggregationOperation(string parameters, Type valuesDataType)
        {
            _valuesDataType = UniversalValue.UniversalClassTypeByType(valuesDataType);
            var percents = parameters.Split(new [] {'|'}, StringSplitOptions.RemoveEmptyEntries).Select(Double.Parse).ToList();
            if (percents.Count == 0)
                throw new Exception("percentiles list is empty");
            _percents = percents;
        }

        private UniversalValue Percentile(IOrderedEnumerable<UniversalValue> sortedData, double p)
        {
            int count = sortedData.Count();
            if (count == 0) return new UniversalValue(0, _valuesDataType);
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
                return new UniversalValue(leftNumber, _valuesDataType);
            else
            {
                double part = n - Math.Floor(n);
                return new UniversalValue(leftNumber + part * (rightNumber - leftNumber), _valuesDataType);
            }
        }

        public AggregationOperationResult Do(IList<UniversalValue> input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            UniversalValue first = input.FirstOrDefault();
            if (first == null)
                throw new Exception("Sequence contains no elements");
            AggregationOperationResult result = new AggregationOperationResult();
            IOrderedEnumerable<UniversalValue> sortedData = input.OrderBy(i => i);
            _percents.ForEach(pp => result.Add(pp.ToString(CultureInfo.InvariantCulture), new UniversalValue(Percentile(sortedData, pp))));
            return result;
        }
    }
}