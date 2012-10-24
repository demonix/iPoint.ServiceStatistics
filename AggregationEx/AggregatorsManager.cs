using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using EventEvaluationLib;

namespace AggregationEx
{
    public class AggregatorsManager
    {
        private readonly ConcurrentDictionary<AggregatorSettings, Aggregator> _aggregators;
        private readonly Action<IList<AggregatedValue>> _onAggregationCompleteAction;

        public AggregatorsManager(Action<IList<AggregatedValue>> onAggregationCompleteAction)
        {
            _onAggregationCompleteAction = onAggregationCompleteAction;
            _aggregators = new ConcurrentDictionary<AggregatorSettings, Aggregator>();
        }

        public void RegisterAggregator(AggregatorSettings aggregatorSettings)
        {
            Aggregator aggregator = new Aggregator(aggregatorSettings);
            _aggregators.TryAdd(aggregatorSettings, aggregator);
            //aggregator.AggregationComplete.Subscribe(_onAggregationCompleteAction);
            //aggregator.Start();
        }

        public void UnRegisterAggregator(AggregatorSettings aggregatorSettings)
        {
            Aggregator aggregator;
            if (_aggregators.TryRemove(aggregatorSettings, out aggregator))
            {
                aggregator.Dispose();
            }
        }
        public void Push(LogEvent logEvent)
        {
            var keys = _aggregators.Keys;
            foreach (AggregatorSettings aggregatorSettings in keys)
            {
                Aggregator aggregator;
                if (_aggregators.TryGetValue(aggregatorSettings, out aggregator))
                {
                    if (aggregator.CanProcess(logEvent))
                        aggregator.Push(logEvent);
                }
            }
        }

       
    }

    public class AggregatorSettings : IEqualityComparer<AggregatorSettings>
    {
        public string CounterCategoryFilter { get; private set; }

        public string CounterNameFilter { get; private set; }

        public string AggregationType { get; private set; }

        public string AggregationParameters { get; private set; }

        public DataType ValuesDataType { get; private set; }

        public AggregatorSettings(string counterCategoryFilter, string counterNameFilter, string aggregationType,
                                  string aggregationParameters, DataType valuesDataType)
        {
            CounterCategoryFilter = counterCategoryFilter;
            CounterNameFilter = counterNameFilter;
            AggregationType = aggregationType;
            AggregationParameters = aggregationParameters;
            ValuesDataType = valuesDataType;
        }

        public bool Equals(AggregatorSettings other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.ValuesDataType == ValuesDataType && Equals(other.AggregationParameters, AggregationParameters) &&
                   Equals(other.AggregationType, AggregationType) && Equals(other.CounterNameFilter, CounterNameFilter) &&
                   Equals(other.CounterCategoryFilter, CounterCategoryFilter);
        }

        public bool Equals(AggregatorSettings x, AggregatorSettings y)
        {
            if (x == null)
                return false;
            return x.Equals(y);
        }

        public int GetHashCode(AggregatorSettings obj)
        {
            unchecked
            {
                int result = (CounterCategoryFilter != null ? CounterCategoryFilter.GetHashCode() : 0);
                result = (result*397) ^ (AggregationType != null ? AggregationType.GetHashCode() : 0);
                result = (result*397) ^ (AggregationParameters != null ? AggregationParameters.GetHashCode() : 0);
                result = (result*397) ^ (ValuesDataType != null ? ValuesDataType.GetHashCode() : 0);
                result = (result*397) ^ (CounterNameFilter != null ? CounterNameFilter.GetHashCode() : 0);
                return result;
            }
        }

    }
}