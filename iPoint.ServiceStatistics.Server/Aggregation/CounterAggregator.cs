using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Aggregation;
using CountersDataLayer;
using EventEvaluationLib;

namespace iPoint.ServiceStatistics.Server.Aggregation
{
    public class CounterAggregator
    {
        public CounterAggregator(string counterCategory, string counterName, AggregationType aggregationType, Type inputType, string percentileAndDistributionParameters)
        {
            if (String.IsNullOrEmpty(counterCategory) || String.IsNullOrEmpty(counterName))
                throw new ArgumentException("counterCategory and counterName must not be empty");

            switch (aggregationType)
            {
                case AggregationType.Percentile:
                    {
                        _percentileParameters =
                            percentileAndDistributionParameters.Split('|').Select(Double.Parse).ToList();
                        break;
                    }
                case AggregationType.ValueDistributionGroups:
                    {

                        var t1 = percentileAndDistributionParameters.Split('|');
                        if (t1.All(t => t.Contains("-")))
                        {
                            _distributionParameters = t1.Select(
                                t =>
                                new Tuple<UniversalValue, UniversalValue>(
                                    UniversalValue.ParseFromString(inputType, t.Split('-')[0]),
                                    UniversalValue.ParseFromString(inputType, t.Split('-')[1]))).ToList();
                        }
                        else
                        {
                            _distributionParameters =
                                FromListToGroup(t1.Select(t => UniversalValue.ParseFromString(inputType, t)).ToList());
                        }



                        break;
                    }

            }
            //_percentileParameters = new List<double>() { 25, 50, 75 };
    
            CounterCategory  = counterCategory;
            CounterName = counterName;
            AggregationType = aggregationType;
            EventSelector = CreateEventSelector();
            AggregationAction = CreateAggregationActionNew();
            InputType = inputType;
            _onResult = OnResult();
            

        }


        List<Tuple<UniversalValue, UniversalValue>>  FromListToGroup(List<UniversalValue> distributionParameters)
        {
            List<Tuple<UniversalValue, UniversalValue>> distributionParametersIntervals = new List<Tuple<UniversalValue, UniversalValue>>();
            UniversalValue first = distributionParameters.First();
            UniversalValue min, max;
            switch (first.Type)
            {
                case UniversalValue.UniversalClassType.Numeric:
                    min = new UniversalValue(Double.MinValue);
                    max = new UniversalValue(Double.MaxValue);
                    break;
                case UniversalValue.UniversalClassType.TimeSpan:
                    min = new UniversalValue(TimeSpan.MinValue);
                    max = new UniversalValue(TimeSpan.MaxValue);
                    break;
                default:
                    throw new Exception("Cannot convert list of string distribution pararameters to intervals");
            }
            distributionParametersIntervals.Add(new Tuple<UniversalValue, UniversalValue>(min,distributionParameters[0]));
            for (int i = 0; i < distributionParameters.Count-1; i++)
            {
                distributionParametersIntervals.Add(new Tuple<UniversalValue, UniversalValue>(distributionParameters[i], distributionParameters[i+1]));
            }
            distributionParametersIntervals.Add(new Tuple<UniversalValue, UniversalValue>(distributionParameters[distributionParameters.Count - 1], max));
            return distributionParametersIntervals;
        }

        private List<double> _percentileParameters;
        private List<Tuple<UniversalValue,UniversalValue>> _distributionParameters;
        public Action<IEnumerable<LogEventArgs>> AggregationAction;
        public Func<LogEventArgs, bool> EventSelector;
        public IDisposable UnsubscriptionToken;
        public string CounterCategory { get; private set; }
        public string CounterName { get; private set; }
        public AggregationType AggregationType { get; private set; }
        public Type InputType { get; private set; }
        private Action<TotalAggregationResult> _onResult;

        /*public CounterAggregator(AggregationParameters aggregationParameters)
            : this(aggregationParameters.CounterCategory, aggregationParameters.CounterName, aggregationParameters.CounterAggregationType, aggregationParameters.CounterType,"")
        {
            
        }*/

       
        private Action<TotalAggregationResult> OnResult()
        {
            return result =>
                       {
                           Console.WriteLine(result.ResultGroups.Count() + " combinations of " + result.CounterCategory + "." +
                                             result.CounterName + " aggregated for " +
                                             DateTime.Now.Subtract(result.Date).TotalMilliseconds/1000d + " seconds. (" +result.Date.TimeOfDay + ")");
                           for (int i = 0; i < 3;i++)
                           {
                               try
                               {
                                   CountersDatabase.Instance.SaveCounters(result);
                                   break;
                               }catch(Exception)
                               {
                                   
                               }

                           }

                       };
        }
        
        public Func<LogEventArgs, bool> CreateEventSelector()
        {
            Expression<Func<LogEventArgs, bool>> exp =
                item =>  (item.LogEvent.Category == CounterCategory) && (item.LogEvent.Counter == CounterName);
            return exp.Compile();


        }

        
        
        private Action<IEnumerable<LogEventArgs>> CreateAggregationActionNew()
        {
            Expression<Action<IEnumerable<LogEventArgs>>> actionResult;// = input => _onResult("empty");
            switch (AggregationType)
            {
                case AggregationType.Sum: 
                actionResult = input => _onResult(new TotalAggregationResult(CounterCategory, CounterName, AggregationType, GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, s.Sum()))));
                    break;
                case AggregationType.Min:
                    actionResult = input => _onResult(new TotalAggregationResult(CounterCategory, CounterName, AggregationType, GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, Enumerable.Min(s)))));
                    break;
                case AggregationType.Max:
                    actionResult = input => _onResult(new TotalAggregationResult(CounterCategory, CounterName, AggregationType, GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, Enumerable.Max(s)))));
                    break;
                case AggregationType.Avg:
                    actionResult = input => _onResult(new TotalAggregationResult(CounterCategory, CounterName, AggregationType, GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, s.Average()))));
                    break;
                case AggregationType.Percentile:
                    actionResult = input => _onResult(new TotalAggregationResult(CounterCategory, CounterName, AggregationType, GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, s.Percentile(_percentileParameters)))));
                    break;
                case AggregationType.ValueDistributionGroups:
                    actionResult = input => _onResult(new TotalAggregationResult(CounterCategory, CounterName, AggregationType, GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, s.Distribution(_distributionParameters)))));
                    break;
                case AggregationType.Count:
                    actionResult = input => _onResult(new TotalAggregationResult(CounterCategory, CounterName, AggregationType, GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, new UniversalValue(s.Count())))));
                    break;
                default:
                    throw new Exception("Unknown aggregationType: " + AggregationType);
            }

            return actionResult.Compile();
        }

       

    
        private IEnumerable<IGrouping<CounterGroup, UniversalValue>> GroupCounters(IEnumerable<LogEventArgs> input)
        {
            return input.GroupBy(k => CreateCounterGroupByMask("111", k),
                                 k =>
                                 UniversalValue.ParseFromString(InputType, k.LogEvent.Value))
                .Concat(
                    input.GroupBy(k => CreateCounterGroupByMask("001", k),
                                  k =>
                                  UniversalValue.ParseFromString(InputType, k.LogEvent.Value)))
                .Concat(
                    input.GroupBy(k => CreateCounterGroupByMask("101", k),
                                  k =>
                                  UniversalValue.ParseFromString(InputType, k.LogEvent.Value)))
                 .Concat(
                    input.GroupBy(k => CreateCounterGroupByMask("010", k),
                                  k =>
                                  UniversalValue.ParseFromString(InputType, k.LogEvent.Value)))
                .Concat(
                    input.GroupBy(k => CreateCounterGroupByMask("110", k),
                                  k =>
                                  UniversalValue.ParseFromString(InputType, k.LogEvent.Value)))
                .Concat(
                    input.GroupBy(k => CreateCounterGroupByMask("100", k),
                                  k =>
                                  UniversalValue.ParseFromString(InputType, k.LogEvent.Value)))
                .Concat(
                    input.GroupBy(k => CreateCounterGroupByMask("000", k),
                                  k =>
                                  UniversalValue.ParseFromString(InputType, k.LogEvent.Value)));
        }

        private CounterGroup CreateCounterGroupByMask(string mask, LogEventArgs eventArgs)
        {
            if (mask.Length !=3) throw new Exception("Mask should be exactly 3 chars length");
            return new CounterGroup(
                eventArgs.LogEvent.Counter,
                mask[0] == '0' ? "ALL_SOURCES" : eventArgs.LogEvent.Source,
                mask[1] == '0' ? "ALL_INSTANCES" : eventArgs.LogEvent.Instance,
                mask[2] == '0' ? "ALL_EXTDATA" : eventArgs.LogEvent.ExtendedData
                );
        }

    
        public void BeginAggregation(IObservable<IList<LogEventArgs>> observableEvents)
        {
            UnsubscriptionToken = observableEvents.ObserveOn(Scheduler.ThreadPool).Subscribe(AggregationAction);
        }

        public bool Equals(CounterAggregator other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.CounterCategory, CounterCategory) && Equals(other.CounterName, CounterName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (CounterAggregator)) return false;
            return Equals((CounterAggregator) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((CounterCategory != null ? CounterCategory.GetHashCode() : 0)*397) ^ (CounterName != null ? CounterName.GetHashCode() : 0);
            }
        }
    }
}