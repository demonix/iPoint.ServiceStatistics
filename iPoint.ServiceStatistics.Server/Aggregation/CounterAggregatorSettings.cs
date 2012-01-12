using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using ExpressionVisualizer;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;
using Microsoft.VisualStudio.DebuggerVisualizers;

namespace iPoint.ServiceStatistics.Server
{
    public class CounterAggregatorSettings
    {
        public CounterAggregatorSettings(string counterCategory, string counterInstance, string counterName, CounterAggregationType aggregationType, Type inputType)
        {
            CounterCategory = counterCategory;
            CounterInstance = counterInstance;
            CounterName = counterName;
            AggregationType = aggregationType;
            EventSelector = CreateEventSelector();
            AggregationAction = CreateAggregationAction();
            _inputType = inputType;
            _parser = CreateParser();
            _onResult = OnResult();
            _percentileParameters = new List<double>() {25,50,75};
        }

        private List<double> _percentileParameters;
        private Action<object> OnResult()
        {
            return
                result =>
                Console.WriteLine("{0}{1}{2}: {3}", CounterCategory,
                                  String.IsNullOrEmpty(CounterName) ? "" : "." + CounterName,
                                  String.IsNullOrEmpty(CounterInstance) ? "" : "@" + CounterInstance, result);
        }

        private Func<string, object> CreateParser()
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
                    result = input => Double.Parse(input.Replace(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator=="."? "," : ".",CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));
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
        }

        private Type _inputType;
        private Func<string, object> _parser;
        private Action<object> _onResult;

        private Action<IEnumerable<EventPattern<LogEventArgs>>> CreateAggregationAction()
        {
            Expression<Action<IEnumerable<EventPattern<LogEventArgs>>>> actionResult = input => _onResult("empty");
            
            switch (AggregationType)
            {
                case CounterAggregationType.Sum:
                    actionResult = input => _onResult(Sum(input));
                    break;
                case CounterAggregationType.Min:
                    actionResult = input => _onResult(Min(input));
                    break;
                case CounterAggregationType.Max:
                    actionResult =  input => _onResult(Max(input));
                    break;
                case CounterAggregationType.Avg:
                    actionResult = input => _onResult(Avg(input));
                    break;
                case CounterAggregationType.Percentile:
                    actionResult = input => _onResult(Percentile(input));
                    break;
            }
            
            return actionResult.Compile();
        }

        private object Sum(IEnumerable<EventPattern<LogEventArgs>> input)
        {
            object result;
            switch (_inputType.Name)
            {
                case "Int32":
                    //result = input.Sum(e => (Int32)_parser(e.EventArgs.LogEvent.Data));

                    result = String.Join(", ",
                                         input.AsParallel().GroupBy(
                                             k =>
                                             new Tuple<string, string, string, string>(
                                                 k.EventArgs.LogEvent.Counter,
                                                 k.EventArgs.LogEvent.Source,
                                                 k.EventArgs.LogEvent.Instance,
                                                 k.EventArgs.LogEvent.ExtendedData),
                                             (k => Int32.Parse(k.EventArgs.LogEvent.Value))).Union(input.AsParallel().GroupBy(
                                             k =>
                                             new Tuple<string, string, string, string>(
                                                 k.EventArgs.LogEvent.Counter,
                                                 "ALL_SOURCES",
                                                 k.EventArgs.LogEvent.Instance,
                                                 k.EventArgs.LogEvent.ExtendedData),
                                             (k => Int32.Parse(k.EventArgs.LogEvent.Value))).Union(
                                             input.AsParallel().GroupBy(
                                             k =>
                                             new Tuple<string, string, string, string>(
                                                 k.EventArgs.LogEvent.Counter,
                                                 k.EventArgs.LogEvent.Source,
                                                 "ALL_INSTANCES",
                                                 k.EventArgs.LogEvent.ExtendedData),
                                             (k => Int32.Parse(k.EventArgs.LogEvent.Value))).Union(
                                             input.AsParallel().GroupBy(
                                             k =>
                                             new Tuple<string, string, string, string>(
                                                 k.EventArgs.LogEvent.Counter,
                                                 k.EventArgs.LogEvent.Source,
                                                 k.EventArgs.LogEvent.Instance,
                                                 "ALL_EXTDATA"),
                                             (k => Int32.Parse(k.EventArgs.LogEvent.Value)))))).
                                             Select(s => String.Format("{0}{1}{2}{3}: {4}", s.Key.Item1, "." + s.Key.Item2, String.IsNullOrEmpty(s.Key.Item3) ?
                "" : "@" + s.Key.Item3, String.IsNullOrEmpty(s.Key.Item4) ? "" : " " + s.Key.Item4, s.Sum())));
                    break;
                case "Int64":
                    result = input.Sum(e => (Int64)_parser(e.EventArgs.LogEvent.Value));
                    break;
                case "Double":
                    result = input.Sum(e => (Double)_parser(e.EventArgs.LogEvent.Value));
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
        }

        private object Avg(IEnumerable<EventPattern<LogEventArgs>> input)
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
        }


        private object Percentile(IEnumerable<EventPattern<LogEventArgs>> input)
        {
            
            PercentileResult result = new PercentileResult();
            
            switch (_inputType.Name)
            {
                case "Int32":
                case "Int64":
                case "Double":
                    _percentileParameters.ForEach(pp => result.Add(pp, Percentile(input.Select(e => (double)_parser(e.EventArgs.LogEvent.Value)).ToArray(), pp)));
                    break;
                case "TimeSpan":
                    _percentileParameters.ForEach(
                        pp =>
                        result.Add(pp,
                                   TimeSpan.FromTicks(
                                       (long)
                                       Percentile(
                                           input.Select(e => (double)((TimeSpan)_parser(e.EventArgs.LogEvent.Value)).Ticks).ToArray().Sort(), pp))));
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
        }
        
        public Action<IEnumerable<EventPattern<LogEventArgs>>> AggregationAction;
        public Func<IList<EventPattern<LogEventArgs>>, IEnumerable<EventPattern<LogEventArgs>>> EventSelector;
        public IDisposable UnsubscriptionToken;

        public string CounterCategory { get; private set; }
        public string CounterInstance { get; private set; }
        public string CounterName { get; private set; }
        public CounterAggregationType AggregationType { get; private set; }

        public Func<IList<EventPattern<LogEventArgs>>, IEnumerable<EventPattern<LogEventArgs>>> CreateEventSelector()
        {
             Func<IList<EventPattern<LogEventArgs>>, IEnumerable<EventPattern<LogEventArgs>>> exp =
                 input => input
                     .Where(e => 
                         (String.IsNullOrEmpty(CounterCategory) ? true: e.EventArgs.LogEvent.Category == CounterCategory) 
                     && (String.IsNullOrEmpty(CounterInstance) ? true: e.EventArgs.LogEvent.Instance == CounterInstance)
                     && (String.IsNullOrEmpty(CounterName) ? true : e.EventArgs.LogEvent.Counter == CounterName));
            //var compiled = exp.Compile();
            return exp;
        }


        public static double Percentile(IList<double> sortedData, double p)
        {
            // algo derived from Aczel pg 15 bottom
            if (sortedData.Count == 0) return 0;
            if (sortedData.Count == 1) return sortedData[sortedData.Count - 1];
            if (p >= 100.0d) return sortedData[sortedData.Count - 1];

            double position = (double)(sortedData.Count + 1) * p / 100.0;
            double leftNumber = 0.0d, rightNumber = 0.0d;

            double n = p / 100.0d * (sortedData.Count - 1) + 1.0d;

            if (position >= 1)
            {
                leftNumber = sortedData[(int)System.Math.Floor(n) - 1];
                rightNumber = sortedData[(int)System.Math.Floor(n)];
            }
            else
            {
                leftNumber = sortedData[0]; // first data
                rightNumber = sortedData[1]; // first data
            }

            if (leftNumber == rightNumber)
                return leftNumber;
            else
            {
                double part = n - System.Math.Floor(n);
                return leftNumber + part * (rightNumber - leftNumber);
            }
        }
        
    }
    
    public enum CounterAggregationType
    {
        Sum,
        Min,
        Max,
        Avg,
        ValueDistributionGroups,
        Percentile
    }
    

}