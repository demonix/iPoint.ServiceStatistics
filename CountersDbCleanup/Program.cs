﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CountersDataLayer;
using CountersDataLayer.CountersCache;


namespace CountersDbCleanup
{
    class Program
    {
        private static Dictionary<string, DateTime> _lastDates = new Dictionary<string, DateTime>();
        static void Main(string[] args)
        {
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
