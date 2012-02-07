using System.Collections.Generic;
using System.Linq;
using System.Threading;
using iPoint.ServiceStatistics.Server.DataLayer;

namespace iPoint.ServiceStatistics.Server.CounterInfo
{
    public class CounterNameInfoOld
    {
        public CounterNameInfoOld(string name, int id, int parentCategoryId)
        {
            ParentCategoryId = parentCategoryId;
            ExtDataInfos = new Dictionary<string, CounterExtDataInfo>();
            ExtDataInfos.Add("ALL_EXTDATA", new CounterExtDataInfo("ALL_EXTDATA",1));
            InstanceInfos = new Dictionary<string, CounterInstanceInfo>();
            InstanceInfos.Add("ALL_INSTANCES", new CounterInstanceInfo("ALL_INSTANCES", 1));
            SourceInfos = new Dictionary<string, CounterSourceInfo>();
            SourceInfos.Add("ALL_SOURCES", new CounterSourceInfo("ALL_SOURCES", 1));
            Name = name;
            Id = id;
            _maxInstId = 1;
            _maxSourceId =1;
            _maxExtDataId = 1;
        }


        private int _maxInstId;
        private int _maxSourceId;
        private int _maxExtDataId;
        
        public int GetNextInstanceId()
        {
            return Interlocked.Increment(ref _maxInstId);
        }
        public int GetNextSourceId()
        {
            return Interlocked.Increment(ref _maxSourceId);
        }
        public int GetNextExtDataId()
        {
            return Interlocked.Increment(ref _maxExtDataId);
        }

    

        public string Name { get; private set; }
        public int Id { get; private set; }
        public int ParentCategoryId { get; private set; }
        public Dictionary<string,CounterSourceInfo> SourceInfos { get; private set; }
        public Dictionary<string,CounterInstanceInfo> InstanceInfos { get; private set; }
        public Dictionary<string,CounterExtDataInfo> ExtDataInfos { get; private set; }


        public CounterNameInfoOld GetCounter(string cn)
        {
            if (NameInfos.ContainsKey(cn))
                return NameInfos[cn];
            lock (NameInfos)
            {
                Reload();
                if (NameInfos.ContainsKey(cn))
                    return NameInfos[cn];
                CreateNew(cn);
                return NameInfos[cn];
            }
        }

       /* private void CreateNew(string cn)
        {
            CounterNameInfoOld counterNameInfo = new CounterNameInfoOld(cn, GetNextCounterNameId(),ParentCategoryId);
            NameInfos.Add(cn, counterNameInfo);
            CountersDatabase.Instance.SaveCounterNameInfo(this, counterNameInfo);
        }
*/
        private void Reload()
        {
            List<CounterNameInfoOld> counterCategoryInfos = CountersDatabase.Instance.GetCounterNames2(Id).ToList();
            NameInfos = counterCategoryInfos.ToDictionary(e => e.Name);
            //NameInfos = counterCategoryInfos.ToDictionary(e => e.Id);
        }

        public CounterSourceInfo GetSource(string cs)
        {

            if (SourceInfos.ContainsKey(cs))
                return SourceInfos[cs];
            lock (SourceInfos)
            {
                Reload();
                if (SourceInfos.ContainsKey(cs))
                    return SourceInfos[cs];
                CreateNewCounterSourceInfo(cs);
                return SourceInfos[cs];
            }
        }

        private void CreateNewCounterSourceInfo(string cs)
        {
            CounterNameInfoOld counterNameInfo = new CounterNameInfoOld(cn, GetNextCounterNameId());
            NameInfos.Add(cn, counterNameInfo);
            CountersDatabase.Instance.SaveCounterNameInfo(this, counterNameInfo);
        }
    }
}