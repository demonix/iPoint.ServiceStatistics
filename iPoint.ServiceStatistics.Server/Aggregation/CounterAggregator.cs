using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using ExpressionVisualizer;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;
using iPoint.ServiceStatistics.Server.Aggregation;
using iPoint.ServiceStatistics.Server.DataLayer;
using Microsoft.VisualStudio.DebuggerVisualizers;

namespace iPoint.ServiceStatistics.Server
{
    public class CounterAggregator
    {
        public CounterAggregator(string counterCategory, string counterName, AggregationType aggregationType, Type inputType/*, CountersDatabase db*/)
        {
            if (String.IsNullOrEmpty(counterCategory) || String.IsNullOrEmpty(counterName))
                throw new ArgumentException("counterCategory and counterName must not be empty");
                
            CounterCategory  = counterCategory;
            CounterName = counterName;
            AggregationType = aggregationType;
            EventSelector = CreateEventSelector();
            AggregationAction = CreateAggregationActionNew();
            InputType = inputType;
            //_parser = CreateParser();
            _onResult = OnResult();
            _percentileParameters = new List<double>() { 25, 50, 75 };
            //_db = db;
        }

        //private CountersDatabase _db;
        private List<double> _percentileParameters;
        public Action<IEnumerable<EventPattern<LogEventArgs>>> AggregationAction;
        public Func<EventPattern<LogEventArgs>, bool> EventSelector;
        public IDisposable UnsubscriptionToken;
        public string CounterCategory { get; private set; }
        public string CounterName { get; private set; }
        public AggregationType AggregationType { get; private set; }
        public Type InputType { get; private set; }
        private Func<string, object> _parser;
        //private Action<IEnumerable<TotalAggregationResult>> _onResult;
        private Action<TotalAggregationResult> _onResult;

        public CounterAggregator(AggregationParameters aggregationParameters/*, CountersDatabase counterDb*/)
            : this(aggregationParameters.CounterCategory, aggregationParameters.CounterName, aggregationParameters.CounterAggregationType, aggregationParameters.CounterType/*, counterDb*/)
        {
            
        }

        /*private Action<IEnumerable<TotalAggregationResult>> OnResult()
        {
            return
                //result => Void();
                result => result.AsParallel().ForAll(r => _db.SaveCounters(r));
            //DateTime.Now, CounterCategory, CounterName, result);
        }*/
        private Action<TotalAggregationResult> OnResult()
        {
            return
                //result => Void();
                result => CountersDatabase.Instance.SaveCounters(result);
            //DateTime.Now, CounterCategory, CounterName, result);
        }
        
        public Func<EventPattern<LogEventArgs>, bool> CreateEventSelector()
        {
            Expression<Func<EventPattern<LogEventArgs>, bool>> exp =
                item =>  (item.EventArgs.LogEvent.Category == CounterCategory) &&
                    //item.EventArgs.LogEvent.Counter.StartsWith(CounterName);
                    (item.EventArgs.LogEvent.Counter == CounterName);
            exp.Compile();
               
            return exp.Compile();
        }

        
        
        private Action<IEnumerable<EventPattern<LogEventArgs>>> CreateAggregationActionNew()
        {
            Expression<Action<IEnumerable<EventPattern<LogEventArgs>>>> actionResult;// = input => _onResult("empty");
            switch (AggregationType)
            {
                case AggregationType.Sum: 
                    //actionResult = input => _onResult(GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, s.Sum())).
                //                      GroupBy(gar => gar.CounterGroup.CounterName).
                //                      Select(gar => new TotalAggregationResult(CounterCategory, gar.Key, AggregationType, gar)));
                actionResult = input => _onResult(new TotalAggregationResult(CounterCategory, CounterName, AggregationType, GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, s.Sum()))));
                    //actionResult = input => _onResult(String.Join("}, {", GroupCountersParallel(input).Select(s => FormatResult(s, new List<Tuple<string, UniversalValue>>() { new Tuple<string, UniversalValue>("value", s.Sum()) }))));
                    //actionResult = input => _onResult(Sum(input));
                    break;
                case AggregationType.Min:
                //    actionResult = input => _onResult(GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, s.Min())).
                //                      GroupBy(gar => gar.CounterGroup.CounterName).
                //                      Select(gar => new TotalAggregationResult(CounterCategory, gar.Key, AggregationType, gar)));
                    actionResult = input => _onResult(new TotalAggregationResult(CounterCategory, CounterName, AggregationType, GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, s.Min()))));
                    //actionResult = input => _onResult(String.Join("}, {", GroupCountersParallel(input).Select(s => FormatResult(s,new List<Tuple<string, UniversalValue>>() {new Tuple<string, UniversalValue>("value", s.Min())}))));
                    break;
                case AggregationType.Max:
                    //actionResult = input => _onResult(GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, s.Max())).
                    //                  GroupBy(gar => gar.CounterGroup.CounterName).
                    //                  Select(gar => new TotalAggregationResult(CounterCategory, gar.Key, AggregationType, gar)));
                    actionResult = input => _onResult(new TotalAggregationResult(CounterCategory, CounterName, AggregationType, GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, s.Max()))));
                    //actionResult = input => _onResult(String.Join("}, {", GroupCountersParallel(input).Select(s => FormatResult(s, new List<Tuple<string, UniversalValue>>() { new Tuple<string, UniversalValue>("value", s.Max()) }))));
                    break;
                case AggregationType.Avg:
                    //actionResult = input => _onResult(GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, s.Average())).
                     //                 GroupBy(gar => gar.CounterGroup.CounterName).
                      //                Select(gar => new TotalAggregationResult(CounterCategory, gar.Key, AggregationType, gar)));
                    actionResult = input => _onResult(new TotalAggregationResult(CounterCategory, CounterName, AggregationType, GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, s.Average()))));
                    //actionResult = input => _onResult(String.Join("}, {", GroupCountersParallel(input).Select(s => FormatResult(s, new List<Tuple<string, UniversalValue>>() { new Tuple<string, UniversalValue>("value", s.Average()) }))));
                    break;
                case AggregationType.Percentile:
                    //actionResult = input => _onResult(GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, s.Percentile(_percentileParameters))).
                    //                  GroupBy(gar => gar.CounterGroup.CounterName).
                     //                 Select(gar => new TotalAggregationResult(CounterCategory, gar.Key, AggregationType, gar)));
                    actionResult = input => _onResult(new TotalAggregationResult(CounterCategory, CounterName, AggregationType, GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, s.Percentile(_percentileParameters)))));
                    //actionResult = input => _onResult(String.Join("}, {", GroupCountersParallel(input).Select(s => FormatResult(s, s.Percentile(_percentileParameters)))));
                    break;
                default:
                    throw new Exception("Unknown aggregationType: " + AggregationType);
            }

            return actionResult.Compile();
        }

       

        /*private ParallelQuery<IGrouping<CounterGroup, UniversalValue>> GroupCountersParallel(IEnumerable<EventPattern<LogEventArgs>> input)
        {

            return input.AsParallel().GroupBy(k => CreateCounterGroupByMask("1111", k),
                                              k =>
                                              UniversalValue.ParseFromString(_inputType, k.EventArgs.LogEvent.Value))
                .Concat(
                input.AsParallel().GroupBy(k => CreateCounterGroupByMask("1011", k),
                                                   k =>
                                                   UniversalValue.ParseFromString(_inputType,
                                                                                  k.EventArgs.LogEvent.Value)))
                .Concat(
                    input.AsParallel().GroupBy(k => CreateCounterGroupByMask("1101",k),
                                               k =>
                                               UniversalValue.ParseFromString(_inputType, k.EventArgs.LogEvent.Value)))
                .Concat(
                    input.AsParallel().GroupBy(k => CreateCounterGroupByMask("1110", k),
                                               k =>
                                               UniversalValue.ParseFromString(_inputType, k.EventArgs.LogEvent.Value)))
                .Concat(
                    input.AsParallel().GroupBy(k => CreateCounterGroupByMask("1000", k),
                                               k =>
                                               UniversalValue.ParseFromString(_inputType, k.EventArgs.LogEvent.Value)));
        }*/

        private IEnumerable<IGrouping<CounterGroup, UniversalValue>> GroupCounters(IEnumerable<EventPattern<LogEventArgs>> input)
        {

            return input.GroupBy(k => CreateCounterGroupByMask("111", k),
                                 k =>
                                 UniversalValue.ParseFromString(InputType, k.EventArgs.LogEvent.Value))
                .Concat(
                    input.GroupBy(k => CreateCounterGroupByMask("011", k),
                                  k =>
                                  UniversalValue.ParseFromString(InputType, k.EventArgs.LogEvent.Value)))
                .Concat(
                    input.GroupBy(k => CreateCounterGroupByMask("101", k),
                                  k =>
                                  UniversalValue.ParseFromString(InputType, k.EventArgs.LogEvent.Value)))
                .Concat(
                    input.GroupBy(k => CreateCounterGroupByMask("110", k),
                                  k =>
                                  UniversalValue.ParseFromString(InputType, k.EventArgs.LogEvent.Value)))
                .Concat(
                    input.GroupBy(k => CreateCounterGroupByMask("100", k),
                                  k =>
                                  UniversalValue.ParseFromString(InputType, k.EventArgs.LogEvent.Value)))
                .Concat(
                    input.GroupBy(k => CreateCounterGroupByMask("000", k),
                                  k =>
                                  UniversalValue.ParseFromString(InputType, k.EventArgs.LogEvent.Value)));
        }

        private CounterGroup CreateCounterGroupByMask(string mask, EventPattern<LogEventArgs> eventPattern)
        {
            if (mask.Length !=3) throw new Exception("Mask should be exactly 3 chars length");
            return new CounterGroup(
                eventPattern.EventArgs.LogEvent.Counter,
                mask[0] == '0' ? "ALL_SOURCES" : eventPattern.EventArgs.LogEvent.Source,
                mask[1] == '0' ? "ALL_INSTANCES" : eventPattern.EventArgs.LogEvent.Instance,
                mask[2] == '0' ? "ALL_EXTDATA" : eventPattern.EventArgs.LogEvent.ExtendedData
                );
        }

        #region old_stuff
        /*  private object SumNew(IEnumerable<EventPattern<LogEventArgs>> input)
  {
      object result;
      switch (_inputType.Name)
      {
          case "Int32":
          case "Int64":
          case "Double":
              result = String.Join(", ", GroupCountersParallel(input).Select(s => FormatResult(s, s.Sum())));
              break;
          case "TimeSpan":
              result = String.Join(", ", GroupCountersParallel(input).Select(s => FormatResult(s, s.Sum())));
              break;
          case "String":
              result = String.Join(", ", GroupCountersParallel(input).Select(s => FormatResult(s, s.Sum())));
              break;
          default:
              throw new Exception("Cannot compute Sum() on " + _inputType.FullName);
      }
      return result;
  }*/
        /* private object Sum(IEnumerable<EventPattern<LogEventArgs>> input)
         {
             object result;
             switch (_inputType.Name)
             {
                 case "Int32":
                 case "Int64":
                 case "Double":
                     result = String.Join(", ",
                                          GroupCountersParallel(input).Select(
                                              s =>
                                              String.Format("{0}{1}{2}{3}: {4}", s.Key.Item1, "." + s.Key.Item2,
                                                            String.IsNullOrEmpty(s.Key.Item3) ? "" : "@" + s.Key.Item3,
                                                            String.IsNullOrEmpty(s.Key.Item4) ? "" : " " + s.Key.Item4,
                                                            (s as IGrouping<Tuple<string, string, string, string>, Double>).Sum())));
                     break;
                 case "TimeSpan":
                     result = TimeSpan.FromTicks(input.Sum(e => ((TimeSpan)_parser(e.EventArgs.LogEvent.Value)).Ticks));
                     break;
                 case "String":
                     result = String.Join(", ", input.Select(e => e.EventArgs.LogEvent.Value));
                     break;
                 default:
                     throw new Exception("Cannot compute Sum() on " + _inputType.FullName);
             }
             return result;
         }*/
        /*private object Avg(IEnumerable<EventPattern<LogEventArgs>> input)
        {
            object result;
            
            switch (_inputType.Name)
            {
                case "Int32":
                    result = input.Count() == 0 ? 0 : input.Average(e => (Int32)_parser(e.EventArgs.LogEvent.Value));
                    break;
                case "Int64":
                    result = input.Count() == 0 ? 0 : input.Average(e => (Int64)_parser(e.EventArgs.LogEvent.Value));
                    break;
                case "Double":
                    result = input.Count() == 0 ? 0 : input.Average(e => (Double)_parser(e.EventArgs.LogEvent.Value));
                    break;
                case "TimeSpan":
                    result = input.Count() == 0 ? "00:00:00" : String.Join(", ", input.GroupBy(k => k.EventArgs.LogEvent.Instance, (e => ((TimeSpan)_parser(e.EventArgs.LogEvent.Value)).Ticks)).Select(s => s.Key + ": " + s.Average()));
                    //result = input.Count() == 0 ? TimeSpan.Zero : TimeSpan.FromTicks((long)input.Average(e => ((TimeSpan)_parser(e.EventArgs.LogEvent.Value)).Ticks));
                    break;
                default:
                    throw new Exception("Cannot compute Avg() on " + _inputType.FullName);
            }
            return result;
        }
        private object Max(IEnumerable<EventPattern<LogEventArgs>> input)
        {
            object result;
            switch (_inputType.Name)
            {
                case "Int32":
                    result = input.Max(e => (Int32)_parser(e.EventArgs.LogEvent.Value));
                    break;
                case "Int64":
                    result = input.Max(e => (Int64)_parser(e.EventArgs.LogEvent.Value));
                    break;
                case "Double":
                    result = input.Max(e => (Double)_parser(e.EventArgs.LogEvent.Value));
                    break;
                case "TimeSpan":
                    result = TimeSpan.FromTicks(input.Max(e => ((TimeSpan)_parser(e.EventArgs.LogEvent.Value)).Ticks));
                    break;
                default:
                    throw new Exception("Cannot compute Max() on " + _inputType.FullName);
            }
            return result;
        }*/
        /* private object Percentile(IEnumerable<EventPattern<LogEventArgs>> input)
         {
            
             PercentileResult result = new PercentileResult();
            
             switch (_inputType.Name)
             {
                 case "Int32":
                 case "Int64":
                 case "Double":
                     _percentileParameters.ForEach(pp => result.Add(pp, Percentile(input.Select(e => (double)_parser(e.EventArgs.LogEvent.Value)).OrderBy(o => o), pp)));
                     break;
                 case "TimeSpan":
                     _percentileParameters.ForEach(
                         pp =>
                         result.Add(pp,
                                    TimeSpan.FromTicks(
                                        (long)
                                        Percentile(
                                            input.Select(e => (double)((TimeSpan)_parser(e.EventArgs.LogEvent.Value)).Ticks).OrderBy(o => o), pp))));
                     break;
                 default:
                     throw new Exception("Cannot compute Percentile() on " + _inputType.FullName);
             }
             return result;
         }
         private object Min(IEnumerable<EventPattern<LogEventArgs>> input)
         {
             object result;
             switch (_inputType.Name)
             {
                 case "Int32":
                     result = input.Min(e => (Int32)_parser(e.EventArgs.LogEvent.Value));
                     break;
                 case "Int64":
                     result = input.Min(e => (Int64)_parser(e.EventArgs.LogEvent.Value));
                     break;
                 case "Double":
                     result = input.Min(e => (Double)_parser(e.EventArgs.LogEvent.Value));
                     break;
                 case "TimeSpan":
                     result = TimeSpan.FromTicks(input.Min(e => ((TimeSpan)_parser(e.EventArgs.LogEvent.Value)).Ticks));
                     break;
                 default:
                     throw new Exception("Cannot compute Min() on " + _inputType.FullName);
             }
             return result;
         }*/
        /* private Action<IEnumerable<EventPattern<LogEventArgs>>> CreateAggregationAction()
         {
             Expression<Action<IEnumerable<EventPattern<LogEventArgs>>>> actionResult = input => _onResult("empty");
            
             switch (AggregationType)
             {
                 case AggregationType.Sum:
                     actionResult = input => _onResult(SumNew(input));
                     //actionResult = input => _onResult(Sum(input));
                     break;
                 case AggregationType.Min:
                     actionResult = input => _onResult(Min(input));
                     break;
                 case AggregationType.Max:
                     actionResult =  input => _onResult(Max(input));
                     break;
                 case AggregationType.Avg:
                     actionResult = input => _onResult(Avg(input));
                     break;
                 case AggregationType.Percentile:
                     actionResult = input => _onResult(Percentile(input));
                     break;
             }
            
             return actionResult.Compile();
         }*/
        /*public static double Percentile(IOrderedEnumerable<double> sortedData, double p)
        {
            // algo derived from Aczel pg 15 bottom
            int count = sortedData.Count();
            if (count == 0) return 0;
            if (count == 1) return sortedData.Last();
            if (p >= 100.0d) return sortedData.Last();

            double position = (count + 1) * p / 100.0;
            double leftNumber, rightNumber;

            double n = p / 100.0d * (count - 1) + 1.0d;

            if (position >= 1)
            {
                leftNumber = sortedData.ElementAt((int)Math.Floor(n) - 1);
                rightNumber = sortedData.ElementAt((int)Math.Floor(n));
            }
            else
            {
                leftNumber = sortedData.First();
                rightNumber = sortedData.ElementAt(1);
            }

            if (leftNumber == rightNumber)
                return leftNumber;
            else
            {
                double part = n - Math.Floor(n);
                return leftNumber + part * (rightNumber - leftNumber);
            }
        }*/
        /*private Func<string, object> CreateParser()
       {
            
           Expression<Func<string, object>> result;
           switch (_inputType.Name)
           {
               case "Int32":
                   result = input => Int32.Parse(input);
                   break;
               case "Int64":
                   result = input => Int64.Parse(input);
                   break;
               case "Double":
                   result = input => Double.Parse(input.Replace(",", "."));
                   break;
               case "TimeSpan":
                   result = input => TimeSpan.Parse(input);
                   break;
               case "String":
                   result = input => input;
                   break;
               default:
                   throw new Exception("Unknown type " + _inputType.FullName);
           }
           return result.Compile();
       }*/
        #endregion old_stuff

        public void BeginAggregation(IObservable<IList<EventPattern<LogEventArgs>>> observableEvents)
        {
            UnsubscriptionToken = observableEvents.Subscribe(AggregationAction);
        }
    }
}