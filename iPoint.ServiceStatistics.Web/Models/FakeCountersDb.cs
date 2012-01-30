using System;
using System.Collections.Generic;
using System.Linq;

namespace iPoint.ServiceStatistics.Web.Models
{
    public class FakeCountersDb
    {
         static List<Tuple<string,string,string,string,string>> fakeBase = new List<Tuple<string, string, string, string, string>>()
                                                                        {
                                                                            new Tuple<string, string, string, string, string>("cat1","Name1","Source1","Instance1","ExtData1"),
                                                                            new Tuple<string, string, string, string, string>("cat1","Name1","Source1","Instance1","ExtData2"),
                                                                            new Tuple<string, string, string, string, string>("cat1","Name1","Source2","Instance2","ExtData1"),
                                                                            new Tuple<string, string, string, string, string>("cat1","Name1","Source2","Instance1","ExtData1"),
                                                                            new Tuple<string, string, string, string, string>("cat1","Name2","Source1","Instance1","ExtData1"),
                                                                            new Tuple<string, string, string, string, string>("cat1","Name2","Source1","Instance1","ExtData1"),
                                                                            new Tuple<string, string, string, string, string>("cat2","Name1","Source1","Instance1","ExtData2"),
                                                                            new Tuple<string, string, string, string, string>("cat2","Name1","Source1","Instance2","ExtData2"),
                                                                            new Tuple<string, string, string, string, string>("cat2","Name1","Source1","Instance1","ExtData1"),
                                                                            new Tuple<string, string, string, string, string>("cat2","Name1","Source1","Instance1","ExtData1"),
                                                                            new Tuple<string, string, string, string, string>("cat1","Name3","Source1","Instance2","ExtData1")
                                                                        };


        public static IEnumerable<string> GetCounterCategories(DateTime beginDate, DateTime endDate)
        {
            return fakeBase.Select(i => i.Item1).Distinct().AsEnumerable();
        }

        public static IEnumerable<string> GetCounterNames(DateTime beginDate, DateTime endDate, string counterCategory)
        {
            return fakeBase.Where(i=> i.Item1 == counterCategory).Select(i => i.Item2).Distinct().AsEnumerable();
        }

        public static IEnumerable<Tuple<string, string, string>> GetCounterDetails(DateTime beginDate, DateTime endDate, string counterCategory, string counterName)
        {
            return fakeBase.Where(i => (i.Item1 == counterCategory) && (i.Item2 == counterName)).Select(i => new Tuple<string, string, string>(i.Item3,i.Item4, i.Item5)).AsEnumerable();
        }
    }
}