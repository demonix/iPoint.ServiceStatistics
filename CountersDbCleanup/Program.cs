using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using CountersDataLayer;
using CountersDataLayer.CountersCache;


namespace CountersDbCleanup
{
    class Program
    {
        private static Dictionary<string, DateTime> _lastDates = new Dictionary<string, DateTime>();
        public Action<IEnumerable<int>> AggregationAction;
        public Action<int> _onResult;
        public Action<DateTime> _onResult2;

        private Action<IEnumerable<int>> CreateAggregationAction(string switcher)
        {
            Console.WriteLine("in action");
            Action<IEnumerable<int>> result;
            switch (switcher)
            {
                case"test":
                    Func<IEnumerable<int>, IEnumerable<int>> selectEvens = all => all.Where(e => e%2 == 0);
                    Func<IEnumerable<int>, int> summarize = elems => elems.Sum();
                    result = input => _onResult(summarize(selectEvens(input)) / input.Count());
                        //_onResult(new TotalAggregationResult(CounterCategory, CounterName, AggregationType, GroupCounters(input.Where(EventSelector)).Select(s => new GroupAggregationResult(s, s.Sum()))));
                    break;
                default:
                    result = input => _onResult2(DateTime.Now);
                    break;
            }
            return result;
        }
        
        void Test()
        {
            _onResult = i => { };
            _onResult2 = i => { Console.WriteLine(i); Thread.Sleep(500);};
            AggregationAction = CreateAggregationAction("test2");
            for (int j = 0; j < 10; j++)
            {


                Stopwatch sw = new Stopwatch();

                sw.Start();
                for (int i = 0; i < 100000; i++)
                {
                    AggregationAction(new int[]
                                          {
                                              1, 45, 345, 6, 2, 5, 6, 123123, 243, 34, 654, 64, 78908, 7, 35, 45654,
                                              3434, 32,
                                              5342, 534, 5623, 45, 315, 345, 34, 53, 6, 346, 234, 5, 3145, 134, 6, 3146,
                                              134,
                                              6324, 6, 2456, 234, 6243, 6, 2546, 542, 62, 5346, 5, 6, 45, 62, 6, 234566,
                                              25,
                                              6245213, 5, 346324, 6, 26, 2, 436, 23, 632, 46, 236, 45346
                                          });
                }
                sw.Stop();
                Console.WriteLine(sw.ElapsedMilliseconds + " ms elapsed");
            }
            Console.ReadLine();

        }

        static void Main(string[] args)
        {
            var list = new List<int> { 1, 2, 3 };
            //var x1 = new { Items = ((IEnumerable<int>)list).GetEnumerator() };
           /* while (x1.Items.MoveNext())
            {
                Console.WriteLine(x1.Items.Current);
            }*/

            //Console.ReadLine();

            var x2 = new { Items = list.GetEnumerator() };
            var en = list.GetEnumerator();
            while (en.MoveNext())
            {
                Console.WriteLine(en.Current);
            }
            Console.ReadLine();
            return;
            
            Program pr = new Program();
            pr.Test();
            return;
            ReadLastDates();
            string mongoUrl = File.ReadAllText("settings\\mongoConnection");
            CountersDatabase.InitConnection(mongoUrl);
            List<CounterCategoryInfo> categories = CountersDatabase.Instance.GetCounterCategories().ToList();
            foreach (CounterCategoryInfo counterCategoryInfo in categories)
            {
                List<CounterNameInfo> counterNames =
                    CountersDatabase.Instance.GetCounterNamesInCategory(counterCategoryInfo.Id).ToList();
                foreach (CounterNameInfo counterNameInfo in counterNames)
                {
                    Console.WriteLine("Чистим " + counterCategoryInfo.Name + "." + counterNameInfo.Name);
                    DateTime left, right;
                    left = right = GetLastProcessedDateForCounter(counterCategoryInfo, counterNameInfo);
                    while (right < DateTime.Now.AddMinutes(-20))
                    {
                        var possibleLeftDate = CountersDatabase.Instance.GetFreshestAfterDate(counterCategoryInfo.Name, counterNameInfo.Name, left);
                        if(!possibleLeftDate.HasValue) break;
                        left = possibleLeftDate.Value;
                        right = left.AddMinutes(5).AddSeconds(-1);
                        CountersDatabase.Instance.RemoveCountersValuesBetweenDates(counterCategoryInfo.Name, counterNameInfo.Name, left, right);
                        //left = right.AddSeconds(1);
                    }
                    Console.WriteLine("Почистили " + counterCategoryInfo.Name + "." + counterNameInfo.Name);
                    File.AppendAllText("lastDates", counterCategoryInfo.Name + "\t" + counterNameInfo.Name + "\t" + left+"\r\n");
                }
            }
        }

       

      

        private static void ReadLastDates()
        {
            string[] lines = File.ReadAllLines("lastDates");
            foreach (string line in lines)
            {
                string[] parts = line.Split('\t');
                if (_lastDates.ContainsKey(parts[0] + "." + parts[1]))
                    _lastDates[parts[0] + "." + parts[1]] = DateTime.Parse(parts[2]);
                else
                    _lastDates.Add(parts[0] + "." + parts[1], DateTime.Parse(parts[2]));
            }
        }

        private static DateTime GetLastProcessedDateForCounter(CounterCategoryInfo counterCategoryInfo, CounterNameInfo counterNameInfo)
        {
            if (_lastDates.ContainsKey(counterCategoryInfo.Name + "." + counterNameInfo.Name))
                return _lastDates[counterCategoryInfo.Name + "." + counterNameInfo.Name];
            else
                return DateTime.MinValue;
        }
    }
}
