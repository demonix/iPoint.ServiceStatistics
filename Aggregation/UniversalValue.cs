using System;
using System.Globalization;

namespace Aggregation
{
    public class UniversalValue : IEquatable<UniversalValue>, IComparable<UniversalValue>
    {
        public enum UniversalClassType
        {
            Numeric,
            TimeSpan,
            String
        }

        public static bool operator >=(UniversalValue a, UniversalValue b)
        {
            return (a > b) || (a == b);
        }

        public static bool operator <=(UniversalValue a, UniversalValue b)
        {
            return (a < b) || (a == b);
        }

        public static bool operator >(UniversalValue a, UniversalValue b)
        {
            if (ReferenceEquals(a, b))
            {
                return false;
            }
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }
            if (a.Type != b.Type) return false;
            switch (a.Type)
            {
                case UniversalClassType.Numeric: return a.DoubleValue > b.DoubleValue;
                case UniversalClassType.TimeSpan:
                    return a.TimespanValue > b.TimespanValue;
                case UniversalClassType.String: return false;
                default:
                    throw new Exception("Unsupported type " + a.Type);
            }
        }

        public static bool operator <(UniversalValue a, UniversalValue b)
        {
            if (ReferenceEquals(a, b))
            {
                return false;
            }
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }
            if (a.Type != b.Type) return false;
            switch (a.Type)
            {
                case UniversalClassType.Numeric: return a.DoubleValue < b.DoubleValue;
                case UniversalClassType.TimeSpan:
                    return a.TimespanValue < b.TimespanValue;
                case UniversalClassType.String: return false;
                default:
                    throw new Exception("Unsupported type " + a.Type);
            }
        }

        public static bool operator ==(UniversalValue a, UniversalValue b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }
            if (a.Type != b.Type) return false;
            switch (a.Type)
            {
                case UniversalClassType.Numeric:
                    return Math.Abs(a.DoubleValue - b.DoubleValue) < Double.Epsilon;
                case UniversalClassType.TimeSpan:
                    return a.TimespanValue == b.TimespanValue;
                case UniversalClassType.String:
                    return a.StringValue == b.StringValue;
                default:
                    throw new Exception("Unsupported type " + a.Type);
            }
        }

        public static bool operator !=(UniversalValue a, UniversalValue b)
        {
            return !(a == b);
        }

        public static implicit operator double(UniversalValue val)
        {
            if (val == null) return 0;
            switch (val.Type)
            {
                case UniversalClassType.Numeric: return val.DoubleValue;
                case UniversalClassType.TimeSpan: return val.TimespanValue.Ticks;
                case UniversalClassType.String: return ParseDouble(val.StringValue);
                default:
                    throw new Exception("Unsupported type " + val.Type);
            }
        }
        
        public UniversalClassType Type { get; private set; }
        public Double DoubleValue { get; private set; }
        public TimeSpan TimespanValue { get; private set; }
        public string StringValue { get; private set; }

        public static UniversalClassType UniversalClassTypeByType(Type type)
        {
            switch (type.Name)
            {
                case "Int32":
                case "Int64":
                case "Double":
                    return UniversalClassType.Numeric;
                case "TimeSpan":
                    return UniversalClassType.TimeSpan;
                case "String":
                    return UniversalClassType.String;
                default:
                    throw new Exception("Unknown type " + type.FullName);
            }
        }


        public static UniversalValue ParseFromString(Type type, string value)
        {
            switch (type.Name)
            {
                case "Int32":
                    return new UniversalValue(Int32.Parse(value));
                case "Int64":
                    return new UniversalValue(Int64.Parse(value));
                case "Double":
                    return new UniversalValue(ParseDouble(value));
                case "TimeSpan":
                    return new UniversalValue(value.Contains(":") ? TimeSpan.Parse(value) : value.ToLower().EndsWith("ms") ? TimeSpan.FromMilliseconds(ParseDouble(value.TrimEnd('m','s',' '))) : TimeSpan.FromSeconds(ParseDouble(value)));
                case "String":
                    return new UniversalValue(value);
                default:
                    throw new Exception("Unknown type " + type.FullName);
            }
        }

        private static double ParseDouble(string value)
        {
            return Double.Parse(value.Replace(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == "." ? "," : ".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));
        }

        public UniversalValue(Double value)
        {
            Type = UniversalClassType.Numeric;
            DoubleValue = value;
        }

        public UniversalValue(Double value, UniversalClassType type)
        {
            Type = type;
            switch (type)
            {
                case UniversalClassType.Numeric:
                    DoubleValue = value;
                    break;
                case UniversalClassType.TimeSpan:
                    TimespanValue = TimeSpan.FromTicks((long) value);
                    break;
                case UniversalClassType.String:
                    StringValue = value.ToString(CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new Exception("Unsupported type " + type);
            }
        }

        public UniversalValue(TimeSpan value)
        {
            Type = UniversalClassType.TimeSpan;
            TimespanValue = value;
        }

        public UniversalValue(string value)
        {
            Type = UniversalClassType.String;
            StringValue = value;
        }


        public int CompareTo(UniversalValue other)
        {
            if (other == null) throw new ArgumentNullException("other");
            if (other.Type != this.Type)
                throw new InvalidOperationException("compare between different types not supported. This: " + this.Type +
                                                    ", other: " + other.Type);
            switch (this.Type)
            {
                case UniversalClassType.Numeric: return this.DoubleValue.CompareTo(other.DoubleValue);
                case UniversalClassType.TimeSpan: return this.TimespanValue.CompareTo(other.TimespanValue);
                case UniversalClassType.String: return String.CompareOrdinal(this.StringValue, other.StringValue);
                default:
                    throw new Exception("Unsupported type " + this.Type);
            }
        }

        public override string ToString()
        {
            switch (Type)
            {
                case UniversalClassType.Numeric:
                    return DoubleValue.ToString(CultureInfo.InvariantCulture);
                case UniversalClassType.TimeSpan:
                    return TimespanValue.ToString();
                case UniversalClassType.String:
                    return StringValue;
                default:
                    throw new Exception("Unknown type " + Type);
            }
        }

        public object AsObject()
        {
            switch (Type)
            {
                case UniversalClassType.Numeric:
                    return DoubleValue;
                case UniversalClassType.TimeSpan:
                    return TimespanValue;
                case UniversalClassType.String:
                    return StringValue;
                default:
                    throw new Exception("Unknown type " + Type);
            }
        }

        public bool Equals(UniversalValue other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Type, Type) && other.DoubleValue.Equals(DoubleValue) && other.TimespanValue.Equals(TimespanValue) && Equals(other.StringValue, StringValue);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (UniversalValue)) return false;
            return Equals((UniversalValue) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = Type.GetHashCode();
                result = (result*397) ^ DoubleValue.GetHashCode();
                result = (result*397) ^ TimespanValue.GetHashCode();
                result = (result*397) ^ (StringValue != null ? StringValue.GetHashCode() : 0);
                return result;
            }
        }
    }
}