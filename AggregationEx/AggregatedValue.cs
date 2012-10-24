using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AggregationEx.AggregationOperations;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace AggregationEx
{
    public class AggregatedValue 
    {
        public DateTime Date { get; private set; }
        public double? Count { get; private set; }
        public double? Sum { get; private set; }
        public double? Min { get; private set; }
        public double? Max { get; private set; }
        public double? Avg { get; private set; }
        public Dictionary<string,double> Percentiles { get; private set; }
        public Dictionary<string, double> DistributionGroups { get; private set; }
        public List<double> RawValues { get; private set; }
        public Dictionary<string, string> Props { get; private set; }


        public AggregatedValue(AggregationKey aggregationKey)
        {
            Date = aggregationKey.Date;
            Props = aggregationKey.Props;
            
        }
        readonly object _locker = new object();
        public void AddRawValues(List<double> rawValues)
        {
            lock (_locker)
            {
                if (RawValues == null)
                    RawValues = new List<double>();
            }
            RawValues.AddRange(rawValues);
        }

        public void AddResult(AggregationOperationResult value)
        {
            switch (value.AggregationType)
            {
                case AggregationType.Max:
                    Max = value.Value[0].Value;
                    break;
                case AggregationType.Min:
                    Min = value.Value[0].Value;
                    break;
                case AggregationType.Avg:
                    Avg = value.Value[0].Value;
                    break;
                case AggregationType.Count:
                    Count = (int) value.Value[0].Value;
                    break;
                case AggregationType.Percentile:
                    Percentiles = value.Value.ToDictionary(e => e.Key, e=>e.Value);
                    break;
                case AggregationType.ValueDistributionGroups:
                    DistributionGroups = value.Value.ToDictionary(e => e.Key, e => e.Value);
                    break;
            }
        }

        public override string ToString()
        {
            return String.Format("Date: {0} ", Date) +
                   String.Format("Props: {0}, ", String.Join(", ", Props.Select(k => k.Key + ": " + k.Value))) +
                   (Count.HasValue ? String.Format("Count: {0}, ", Count) : "") +
                   (Sum.HasValue ? String.Format("Sum: {0}, ", Sum) : "") +
                   (Min.HasValue ? String.Format("Min: {0}, ", Min) : "") +
                   (Max.HasValue ? String.Format("Max: {0}, ", Max) : "") +
                   (Avg.HasValue ? String.Format("Avg: {0}, ", Avg) : "")+
                   (Percentiles !=null ? String.Format("Percentiles: {0}, ", String.Join(", ", Percentiles.Select(p=> p.Key +": "+p.Value))) : "") +
                   (DistributionGroups != null ? String.Format("DistributionGroups: {0}, ", String.Join(", ", DistributionGroups.Select(p=> p.Key +": "+p.Value))) : "");
        }

       
    }
}