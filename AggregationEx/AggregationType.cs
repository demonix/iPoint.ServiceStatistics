using System;
using System.Collections.Generic;

namespace AggregationEx
{
    public enum AggregationType
    {
        Sum,
        Min,
        Max,
        Avg,
        ValueDistributionGroups,
        Percentile,
        Count
    }

    public static class AggregationTypeParser
    {
        private static Dictionary<string, AggregationType> _dic;

        static AggregationTypeParser()
        {
            _dic = new Dictionary<string, AggregationType>();
            foreach (AggregationType at in Enum.GetValues(typeof (AggregationType)))
            {
                _dic.Add(at.ToString().ToLower(), at);
            }
        }

        public static AggregationType Parse(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (_dic.ContainsKey(value.ToLower()))
                return _dic[value.ToLower()];
            throw new ArgumentException(value + " is not correct AggregationType");
        }

        public static bool TryParse(string value, out AggregationType aggregationType)
        {
            aggregationType = AggregationType.Count;
            if (value == null)
                return false;
            if (_dic.ContainsKey(value.ToLower()))
            {
                aggregationType = _dic[value.ToLower()];
                return true;

            }
            return false;
        }


    }

    public static class AggregationTypeExtensions
    {
        private static Dictionary<AggregationType,string> _dic;

        static AggregationTypeExtensions()
        {
            _dic = new Dictionary<AggregationType, string>();
            foreach (AggregationType at in Enum.GetValues(typeof(AggregationType)))
            {
                _dic.Add(at, at.ToString());
            }
        }

        public static string GetName(this AggregationType at)
        {
            return _dic[at];
        }
    }
}