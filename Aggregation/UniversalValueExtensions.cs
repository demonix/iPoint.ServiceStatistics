using System;
using System.Collections.Generic;
using System.Linq;

namespace Aggregation
{
    public static class UniversalValueExtensions
    {

        private static double Percentile(IOrderedEnumerable<double> sortedData, double p)
        {
            int count = sortedData.Count();
            if (count == 0) return 0;
            if (count == 1) return sortedData.Last();
            if (p >= 100.0d) return sortedData.Last();

            double position = (count + 1) * p / 100.0;
            double leftNumber, rightNumber;

            double n = p / 100.0d * (count - 1) + 1.0d;

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

            if (leftNumber == rightNumber)
                return leftNumber;
            else
            {
                double part = n - Math.Floor(n);
                return leftNumber + part * (rightNumber - leftNumber);
            }
        }

        private static double Percentile(IOrderedEnumerable<long> sortedData, double p)
        {
            int count = sortedData.Count();
            if (count == 0) return 0;
            if (count == 1) return sortedData.Last();
            if (p >= 100.0d) return sortedData.Last();

            double position = (count + 1) * p / 100.0;
            long leftNumber, rightNumber;

            double n = p / 100.0d * (count - 1) + 1.0d;

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

            if (leftNumber == rightNumber)
                return leftNumber;
            else
            {
                double part = n - Math.Floor(n);
                return leftNumber + part * (rightNumber - leftNumber);
            }
        }

       
        public static IEnumerable<Tuple<string, UniversalValue>> Distribution(this IEnumerable<UniversalValue> input, List<Tuple<UniversalValue,UniversalValue>> groups)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            UniversalValue first = input.FirstOrDefault();
            double totalCount = input.Count();
            if (first == null)
                throw new Exception("Sequence contains no elements");
            if (first.Type != groups.First().Item1.Type)
                throw new Exception("Input type not equals to group type");

            switch (first.Type)
            {
                case UniversalValue.UniversalClassType.Numeric:
                    return groups.AsParallel().AsOrdered().Select(
                        g =>
                        new Tuple<string, UniversalValue>(("From " + g.Item1 + " to " + g.Item2).Replace(".",","),
                                                          new UniversalValue(
                                                              (input.Count(i => i.DoubleValue >= g.Item1.DoubleValue && i.DoubleValue < g.Item2.DoubleValue)
                                                              / totalCount)*100)));
                case UniversalValue.UniversalClassType.String:
                    return groups.AsParallel().AsOrdered().Select(
                        g =>
                        new Tuple<string, UniversalValue>(g.Item1.StringValue,
                                                          new UniversalValue(
                                                              (input.Count(i => i.StringValue == g.Item1.StringValue)
                                                              / totalCount) * 100)));
                case UniversalValue.UniversalClassType.TimeSpan:
                    return groups.AsParallel().AsOrdered().Select(
                        g =>
                            new Tuple<string, UniversalValue>(("From " + g.Item1 + " to " + g.Item2).Replace(".",","),
                                                          new UniversalValue(
                                                              (input.Count(i => i.TimespanValue >= g.Item1.TimespanValue && i.TimespanValue < g.Item2.TimespanValue)
                                                              / totalCount) * 100)));
                default:
                    throw new Exception("Percentile operarion not supported for " + first.Type);

            }
        }


        public static IEnumerable<Tuple<string,UniversalValue>> Percentile(this IEnumerable<UniversalValue> input, List<double> percents)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            UniversalValue first = input.FirstOrDefault();
            if (first == null)
                throw new Exception("Sequence contains no elements");
            switch (first.Type)
            {
                case UniversalValue.UniversalClassType.Numeric:

                    return
                        percents.AsParallel().AsOrdered().Select(
                            pp =>
                            new Tuple<string,UniversalValue>(pp.ToString(),new UniversalValue(Percentile(input.Select(i => i.DoubleValue).OrderBy(i => i), pp))));

                case UniversalValue.UniversalClassType.TimeSpan:
                    return
                        percents.AsParallel().AsOrdered().Select(
                            pp =>
                            new Tuple<string,UniversalValue>(pp.ToString(),new UniversalValue(
                                TimeSpan.FromTicks(
                                    (long) Percentile(input.Select(i => i.TimespanValue.Ticks).OrderBy(i => i), pp)))));
                default:
                    throw new Exception("Percentile operarion not supported for " + first.Type);
            }
        }

        public static UniversalValue Sum(this IEnumerable<UniversalValue> input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            UniversalValue first = input.FirstOrDefault();
            if (first == null)
                throw new Exception("Sequence contains no elements");
            switch (first.Type)
            {
                case UniversalValue.UniversalClassType.Numeric: return new UniversalValue(input.Sum(i => i.DoubleValue));
                case UniversalValue.UniversalClassType.TimeSpan: return new UniversalValue(TimeSpan.FromTicks(input.Sum(i => i.TimespanValue.Ticks)));
                case UniversalValue.UniversalClassType.String: return new UniversalValue(String.Join(", ", input.Select(i => i.StringValue)));
                default: throw new Exception("Sum operarion not supported for " + first.Type);
            }
        }

        public static UniversalValue Average(this IEnumerable<UniversalValue> input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            UniversalValue first = input.FirstOrDefault();
            if (first == null)
                throw new Exception("Sequence contains no elements");
            switch (first.Type)
            {
                case UniversalValue.UniversalClassType.Numeric: return new UniversalValue(input.Average(i => i.DoubleValue));
                case UniversalValue.UniversalClassType.TimeSpan: return new UniversalValue(TimeSpan.FromTicks((long)input.Average(i => i.TimespanValue.Ticks)));
                default: throw new Exception("Average operarion not supported for " + first.Type);
            }
        }
        
        public static UniversalValue Max(this IEnumerable<UniversalValue> input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            UniversalValue first = input.FirstOrDefault();
            if (first == null)
                throw new Exception("Sequence contains no elements");
            switch (first.Type)
            {
                case UniversalValue.UniversalClassType.Numeric: return new UniversalValue(input.Max(i => i.DoubleValue));
                case UniversalValue.UniversalClassType.TimeSpan: return new UniversalValue(input.Max(i => i.TimespanValue));
                case UniversalValue.UniversalClassType.String: return new UniversalValue(input.Max(i => i.StringValue));
                default: throw new Exception("Max operarion not supported for " + first.Type);
            }
        }

        public static UniversalValue Min(this IEnumerable<UniversalValue> input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            UniversalValue first = input.FirstOrDefault();
            if (first == null)
                throw new Exception("Sequence contains no elements");
            switch (first.Type)
            {
                case UniversalValue.UniversalClassType.Numeric: return new UniversalValue(input.Min(i => i.DoubleValue));
                case UniversalValue.UniversalClassType.TimeSpan: return new UniversalValue(input.Min(i => i.TimespanValue));
                case UniversalValue.UniversalClassType.String: return new UniversalValue(input.Min(i => i.StringValue));
                default: throw new Exception("Min operarion not supported for " + first.Type);
            }
        }

       
    }
}