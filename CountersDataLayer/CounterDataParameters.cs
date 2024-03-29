using System;
using System.Collections.Generic;
using System.Linq;
using CountersDataLayer.CountersCache;

namespace CountersDataLayer
{
    public class CounterDataParameters
    {
        public CounterDataParameters(string sd, string ed, int cc, int cn, int cs, int ci, int ced, string series)
        {
            BeginDate = Helpers.ParseDate(sd, DateTime.Now.AddHours(-1));
            EndDate = Helpers.ParseDate(ed, DateTime.Now.AddHours(1));
            CounterCategoryId = cc;
            CounterNameId = cn;
            CounterSourceId = cs;
            CounterInstanceId = ci;
            CounterExtendedDataId = ced;
            Series = series.Split('|').ToList();
            Sources =
                CountersDatabase.Instance.GetCounterSources(cc, cn).Where(
                    d => (cs == -1 && d.Name != "ALL_SOURCES") || (cs != -1 && d.Id == cs)).ToList();
            Instances =
                CountersDatabase.Instance.GetCounterInstances(cc, cn).Where(
                    d => (ci == -1 && d.Name != "ALL_INSTANCES") || (ci != -1 && d.Id == ci)).ToList();
            ExtendedDatas =
                CountersDatabase.Instance.GetCounterExtDatas(cc, cn).Where(
                    d => (ced == -1 && d.Name != "ALL_EXTDATA") || (ced != -1 && d.Id == ced)).ToList();
        }





        public DateTime BeginDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public int CounterCategoryId { get; private set; }
        public int CounterNameId { get; private set; }
        public int CounterSourceId { get; private set; }
        public int CounterInstanceId { get; private set; }
        public int CounterExtendedDataId { get; private set; }
        public List<string> Series { get; private set; }
        public List<CounterExtDataInfo> ExtendedDatas { get; private set; }
        public List<CounterInstanceInfo> Instances { get; private set; }
        public List<CounterSourceInfo> Sources { get; private set; }



    }
}