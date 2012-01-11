using System;
using System.Collections.Generic;
using System.Text;

namespace iPoint.ServiceStatistics.Server
{
    public class PercentileResult
    {
        private SortedDictionary<double, object> _result;

        public PercentileResult()
        {
            _result = new SortedDictionary<double, object>();
        }

        public void Add(double percent, object value)
        {
            _result.Add(percent, value);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<double, object> keyValuePair in _result)
            {
                sb.AppendFormat("{0}: {1}, ", keyValuePair.Key, keyValuePair.Value);
            }
            return sb.ToString().TrimEnd(',', ' ');
        }
    }
}