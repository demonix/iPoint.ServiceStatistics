using System;
using System.Collections.Generic;
using System.Globalization;
using iPoint.ServiceStatistics.Server.Aggregation;

namespace iPoint.ServiceStatistics.Server.DataLayer
{
    public class CounterSeriesData
    {
        public string SeriesName;
        public List<SeriesPoint> Points = new List<SeriesPoint>();
        private bool lastPointWasNull = false;
        public UniversalValue.UniversalClassType ValueType;
        public string CounterCategory;
        public string CounterName;
        public string CounterSource;
        public string CounterInstance;
        public string CounterExtData;

        public CounterSeriesData(string seriesName, UniversalValue.UniversalClassType valueType, string counterCategory, string counterName, string counterSource, string counterInstance, string counterExtData)
        {
            SeriesName = seriesName;
            ValueType = valueType;
            CounterCategory = counterCategory;
            CounterName = counterName;
            CounterSource = counterSource;
            CounterInstance = counterInstance;
            CounterExtData = counterExtData;
        }

        public void AddSeriesPoint(SeriesPoint seriesPoint)
        {
            if (seriesPoint == null && lastPointWasNull)
                return;
            lastPointWasNull = seriesPoint == null;
            Points.Add(seriesPoint);
        }

        public string UniqId { get { return (CounterCategory+CounterName+CounterSource+CounterInstance+CounterExtData+SeriesName).GetHashCode().ToString(CultureInfo.InvariantCulture); } }
    }

    public class SeriesPoint
    {
        public DateTime DateTime;
        public double Value;

        public SeriesPoint(DateTime dateTime, UniversalValue value)
        {
            DateTime = dateTime;
            Value = value.Type == UniversalValue.UniversalClassType.TimeSpan? value.TimespanValue.Milliseconds: value.DoubleValue;
        }
    }
}