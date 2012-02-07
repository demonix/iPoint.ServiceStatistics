using System;
using System.Collections;

namespace iPoint.ServiceStatistics.Server.Aggregation
{
    public class CounterGroup
    {

        public string Source { get; private set; }
        public string CounterName { get; private set; }
        public string Instance { get; private set; }
        public string ExtendedData { get; private set; }
        private readonly int _hashCode = Int32.MinValue;


        public CounterGroup(string counterName, string source, string instance, string extendedData)
        {
            CounterName = counterName;
            Source = source;
            Instance = instance;
            ExtendedData = extendedData;
            _hashCode = ComputeHashCode();
        }


        private int ComputeHashCode()
        {
            unchecked
            {
                int result = (Source != null ? Source.GetHashCode() : 0);
                result = (result * 397) + (Instance != null ? Instance.GetHashCode() : 0);
                result = (result * 397) + (ExtendedData != null ? ExtendedData.GetHashCode() : 0);
                result = (result * 397) + (CounterName != null ? CounterName.GetHashCode() : 0);
                return result;
            }
        }

        public bool Equals(CounterGroup other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                (other.Source == Source)
                && (other.Instance == Instance)
                && (other.ExtendedData == ExtendedData)&&
                (other.CounterName == CounterName);

        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (CounterGroup)) return false;
            return Equals((CounterGroup) obj);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public static bool operator ==(CounterGroup left, CounterGroup right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CounterGroup left, CounterGroup right)
        {
            return !Equals(left, right);
        }
    }
}