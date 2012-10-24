using System;
using System.Collections.Generic;
using System.Linq;

namespace AggregationEx
{
    public class AggregationKey : IEquatable<AggregationKey>
    {
        public AggregationKey(DateTime date, Dictionary<string, string> props)
        {
            Date = date;
            Props = props;
        }

        public DateTime Date { get; private set; }
        public Dictionary<String, string> Props { get; private set; }


        public bool Equals(AggregationKey other)
        {
            if (this.Date != other.Date)
                return false;
            if (this.Props.Count != other.Props.Count)
                return false;
            if (!this.Props.All(k => other.Props.ContainsKey(k.Key) && other.Props[k.Key] == k.Value))
                return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AggregationKey) obj);
        }

        public override int GetHashCode()
        {

            return (int) Date.Ticks ^(Props.Count+1);


        }

//        List<KeyValuePair<string,string>> 
    }
}