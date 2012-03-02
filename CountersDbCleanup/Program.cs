using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using iPoint.ServiceStatistics.Server.DataLayer;
using iPoint.ServiceStatistics.Server.КэшСчетчиков;

namespace CountersDbCleanup
{
    class Program
    {
        private static Dictionary<string, DateTime> _lastDates = new Dictionary<string, DateTime>();
        static void Main(string[] args)
        {
            ReadLastDates();
            CountersDatabase.InitConnection("91.142.140.253", null, "counters");
            MongoCollection<BsonDocument> items = CountersDatabase.Instance.Database.GetCollection("countersData");
            List<CounterCategoryInfo> categories = CountersDatabase.Instance.New_GetCounterCategories().ToList();
            foreach (CounterCategoryInfo counterCategoryInfo in categories)
            {
                List<CounterNameInfo> counterNames =
                    CountersDatabase.Instance.New_GetCounterNamesInCategory(counterCategoryInfo.Id).ToList();
                foreach (CounterNameInfo counterNameInfo in counterNames)
                {
                    Console.WriteLine("Чистим " + counterCategoryInfo.Name + "." + counterNameInfo.Name);
                    DateTime left, right;
                    left = right = GetLastProcessedDateForCounter(counterCategoryInfo, counterNameInfo);
                    while (right < DateTime.Now.AddMinutes(-20))
                    {
                        IMongoQuery sq = Query.And(Query.GT("date", left),
                                                   Query.EQ("counterCategory", counterCategoryInfo.Name),
                                                   Query.EQ("counterName", counterNameInfo.Name));
                        var cursor = items.Find(sq);
                        cursor.Limit = 1;
                        cursor.SetFields("date");
                        if (cursor.Size() == 0)
                            break;

                        left = cursor.First()["date"].AsDateTime;
                        right = left.AddMinutes(5).AddSeconds(-1);
                        IMongoQuery dq = Query.And(Query.GT("date", left).LT(right),
                                                   Query.EQ("counterCategory", counterCategoryInfo.Name),
                                                   Query.EQ("counterName", counterNameInfo.Name));
                        items.Remove(dq, SafeMode.True);
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
