using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AggregationEx.AggregationOperations;
using EventEvaluationLib;
using Timer = System.Timers.Timer;

namespace AggregationEx
{
    public class Aggregator: IDisposable
    {
        private readonly string _counterCategoryFilter;
        private readonly string _counterNameFilter;

        Timer _aggreagtionTimer;

        ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
        private readonly List<IAggregationOperation> _aggregationOperations;
        private Action<AggregatedValue> _onAggregateCompleteAction;
        private ConcurrentDictionary<AggregationKey, ConcurrentBag<double>> _events = new ConcurrentDictionary<AggregationKey, ConcurrentBag<double>>();
        ConcurrentBag<Tuple<AggregationKey, double>> _bag = new ConcurrentBag<Tuple<AggregationKey, double>>();

        TimeSpan _aggregationPeriod = TimeSpan.FromSeconds(30);

        public Aggregator(string counterCategoryFilter, string counterNameFilter, List<string> aggregationTypes, Dictionary<string,string> aggregationParameters, Action<AggregatedValue> onAggregateComplete)
        {
            _aggregationOperations = new List<IAggregationOperation>();
            foreach (var aggregationType in aggregationTypes)
            {
                _aggregationOperations.Add(
                    AggregationOperationFactory.Create(aggregationType,
                                                       (aggregationParameters == null || !aggregationParameters.ContainsKey(aggregationType))
                                                           ? ""
                                                           : aggregationParameters[
                                                               aggregationType]));
            }

            _aggreagtionTimer = new Timer(_aggregationPeriod.TotalMilliseconds);
            _aggreagtionTimer.Elapsed += OnAggregationTimer2;
            _counterCategoryFilter = counterCategoryFilter;
            _counterNameFilter = counterNameFilter;
            _onAggregateCompleteAction = onAggregateComplete;
            _aggreagtionTimer.Start();
        }

        private void OnAggregationTimer2(object sender, ElapsedEventArgs e)
        {
            int valuesNumber = 0;
            Console.WriteLine("Begin calc");
            Stopwatch sw = Stopwatch.StartNew();
            Stopwatch waitSw = new Stopwatch();
            DateTime d = DateTime.Now.RoundTo(_aggregationPeriod).Add(-_aggregationPeriod);
            var newBag = new ConcurrentBag<Tuple<AggregationKey, double>>();
            _rwLock.EnterWriteLock();
            ConcurrentBag<Tuple<AggregationKey, double>> tmp;
            try
            {
                tmp = Interlocked.Exchange(ref _bag, newBag);
            }
            finally
            {
                _rwLock.ExitWriteLock();
                waitSw.Stop();
            }
            Dictionary<AggregationKey, List<double>> dic = new Dictionary<AggregationKey, List<double>>();
            foreach (Tuple<AggregationKey, double> tuple in tmp)
            {
                List<double> lst;
                if (dic.TryGetValue(tuple.Item1, out lst))
                    lst.Add(tuple.Item2);
                else
                    dic.Add(tuple.Item1, new List<double>{tuple.Item2});
            }

            List<Task> tasks = new List<Task>();
            foreach (var kvp in dic)
            {
                var localkvp = kvp;
                valuesNumber += localkvp.Value.Count;
                tasks.Add(Task<AggregatedValue>.Factory.StartNew(() => Calculate(localkvp.Key, localkvp.Value, d), CancellationToken.None,
                                                TaskCreationOptions.None, PrioritySheduler.AboveNormal)
                    //.ContinueWith(t => _onAggregateCompleteAction(t.Result))
                                                );

            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            sw.Stop();
            Console.WriteLine("Aggregated and saved {0} groups with  total of {3} values in {1} ms. Wait time: {2}", tmp.Count, sw.ElapsedMilliseconds, waitSw.ElapsedMilliseconds, valuesNumber);
            if (tmp.Count < 10)
                Console.WriteLine(String.Join("\r\n", dic.Keys.Select(k => "Date: " + k.Date + ", " + String.Join(", ", k.Props.Select(p => p.Key + ": " + p.Value)))));
        }

        private void OnAggregationTimer(object sender, ElapsedEventArgs e)
        {
            int valuesNumber = 0;
            Console.WriteLine("Begin calc");
            Stopwatch sw = Stopwatch.StartNew();
            Stopwatch waitSw = new Stopwatch();
            DateTime d = DateTime.Now.RoundTo(_aggregationPeriod).Add(-_aggregationPeriod);
            var newDic = new ConcurrentDictionary<AggregationKey, ConcurrentBag<double>>();
            waitSw.Start();
            _rwLock.EnterWriteLock();
            ConcurrentDictionary<AggregationKey, ConcurrentBag<double>> tmp;
            try
            {
                tmp = Interlocked.Exchange(ref _events, newDic);
            }
            finally
            {
                _rwLock.ExitWriteLock();
                waitSw.Stop();
            }
            List<Task> tasks = new List<Task>();
            foreach (var kvp in tmp)
            {
                var localkvp = kvp;
                valuesNumber += localkvp.Value.Count;
                tasks.Add(Task<AggregatedValue>.Factory.StartNew(() => Calculate(localkvp.Key, localkvp.Value.ToList(), d), CancellationToken.None,
                                                TaskCreationOptions.None, PrioritySheduler.AboveNormal)
                                                //.ContinueWith(t => _onAggregateCompleteAction(t.Result))
                                                );

            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            sw.Stop();
            Console.WriteLine("Aggregated and saved {0} groups with  total of {3} values in {1} ms. Wait time: {2}", tmp.Count, sw.ElapsedMilliseconds, waitSw.ElapsedMilliseconds, valuesNumber);
            if (tmp.Count < 10)
                Console.WriteLine( String.Join("\r\n", tmp.Keys.Select(k => "Date: " +k.Date + ", "+  String.Join(", ", k.Props.Select(p => p.Key + ": " + p.Value)))));
        }

        private AggregatedValue Calculate(AggregationKey key, List<double> valuesList, DateTime d)
        {
            try
            {
                if (key.Date > d)
                {
                    foreach (var value in valuesList)
                    {
                        _bag.Add(new Tuple<AggregationKey, double>(key,value));
                        //_events.GetOrAdd(kvp.Key, new ConcurrentBag<double>()).Add(value);
                    }
                    return null;
                }

                var av = new AggregatedValue(key);
                
                foreach (var aggregationOperation in _aggregationOperations)
                {
                    try
                    {
                        av.AddResult(aggregationOperation.Do(valuesList));
                    }
                    catch (InvalidOperationException)
                    {
                    }

                }
                //TODO dirty hack
                if (
                    _aggregationOperations.Any(
                        op =>
                        op.GetType() == typeof(PercentileAggregationOperation) ||
                        op.GetType() == typeof(DistributionGroupAggregationOperation)))
                    av.AddRawValues(valuesList);
                return av;
                //_onAggregateCompleteAction(av);

                /*ThreadPool.QueueUserWorkItem(state =>
                                                 {
                                                     Thread.Sleep(500);
                                                     int countAfter = kvp.Value.Count;
                                                     int i;
                                                     if (countBefore != countAfter)
                                                         i = 1;
                                                 });
*/

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
            return null;

        }


        AggregatedValue CalculateOld(KeyValuePair<AggregationKey, ConcurrentBag<double>> kvp, DateTime d)
        {
            //int countBefore = kvp.Value.Count;
            try
            {
                if (kvp.Key.Date > d)
                {
                    foreach (var value in kvp.Value)
                    {
                        _events.GetOrAdd(kvp.Key, new ConcurrentBag<double>()).Add(value);
                    }
                    return null;
                }

                var av = new AggregatedValue(kvp.Key);
                List<double> rawValues = kvp.Value.ToList();
                foreach (var aggregationOperation in _aggregationOperations)
                {
                    try
                    {
                        av.AddResult(aggregationOperation.Do(rawValues));
                    }
                    catch (InvalidOperationException)
                    {
                    }

                }
                //TODO dirty hack
                if (
                    _aggregationOperations.Any(
                        op =>
                        op.GetType() == typeof (PercentileAggregationOperation) ||
                        op.GetType() == typeof (DistributionGroupAggregationOperation)))
                    av.AddRawValues(rawValues);
                return av;
                //_onAggregateCompleteAction(av);

                /*ThreadPool.QueueUserWorkItem(state =>
                                                 {
                                                     Thread.Sleep(500);
                                                     int countAfter = kvp.Value.Count;
                                                     int i;
                                                     if (countBefore != countAfter)
                                                         i = 1;
                                                 });
*/

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
            return null;
        }

        public Aggregator(AggregatorSettings aggregatorSettings)
            : this(aggregatorSettings.CounterCategoryFilter, aggregatorSettings.CounterNameFilter, new List<string>(){aggregatorSettings.AggregationType}, new Dictionary<string, string>(){{aggregatorSettings.AggregationType, aggregatorSettings.AggregationParameters}}, _=>{})
        {
            
        }

        public bool CanProcess(LogEvent logEvent)
        {
            return logEvent.Category == _counterCategoryFilter && logEvent.Counter == _counterNameFilter;
        }
        
       
        public void Push(LogEvent logEvent)
        {
            var props = new Dictionary<string, string>();
            props["cCat"] = logEvent.Category;
            props["cName"] = logEvent.Counter;
            props["src"] = logEvent.Source;
            if (!String.IsNullOrEmpty(logEvent.Instance)) props["instance"] = logEvent.Instance;
            if (!String.IsNullOrEmpty(logEvent.ExtendedData)) props["ext_data"] = logEvent.ExtendedData;
            var aggregationKey = new AggregationKey(logEvent.DateTime.RoundTo(_aggregationPeriod), props);
            /*_events.AddOrUpdate(aggregationKey,
                                new ConcurrentBag<double>() {SlowPokeParse(logEvent)},
                                (key, bag) =>
                                    {
                                        bag.Add(SlowPokeParse(logEvent));
                                        return bag;
                                    });
            */
            var value = Double.Parse(logEvent.Value);
            //_rwLock.EnterReadLock();
            try
            {
                _bag.Add(new Tuple<AggregationKey, double>(aggregationKey, value));
                //_events.GetOrAdd(aggregationKey, new ConcurrentBag<double>()).Add(value);
            }
            finally
            {
              //  _rwLock.ExitReadLock();
            }

        }

        private static double SlowPokeParse(LogEvent logEvent)
        {
            Thread.Sleep(50);
            return Double.Parse(logEvent.Value);
        }

        public void Dispose()
        {
            _aggreagtionTimer.Dispose();
        }
    }

    public enum DataType
    {
        Numeric,
        TimeSpan
    }
}