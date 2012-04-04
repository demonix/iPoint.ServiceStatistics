using System;
using System.Globalization;

namespace iPoint.ServiceStatistics.Server.Aggregation
{
    public class UniversalValue
    {
        public enum UniversalClassType
        {
            Numeric,
            TimeSpan,
            String
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
                    return new UniversalValue(value.Contains(":")? TimeSpan.Parse(value):TimeSpan.FromSeconds(ParseDouble(value)));
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
    }
}