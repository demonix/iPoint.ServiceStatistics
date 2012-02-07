using System.Collections.Generic;
using System.Linq;
using System.Threading;
using iPoint.ServiceStatistics.Server.CounterInfo.Stores;
using iPoint.ServiceStatistics.Server.DataLayer;

namespace iPoint.ServiceStatistics.Server.CounterInfo
{
    public class CounterCategoryInfoOld
    {

        public CounterCategoryInfoOld(string name, int id)
        {
            CounterNameInfoStore _store = new CounterNameInfoStore(id);
            Name = name;
            Id = id;
        }

        public CounterNameInfoOld GetCounter(string cn)
        {
            _store.
        }
        
        public string Name { get; private set; }
        public int Id { get; private set; }
        

       /* public string GetMappedCounterName(int counterId)
        {
            if (!NameReverseInfos.ContainsKey(counterId))
                lock (NameReverseInfos)
                {
                    NameReverseInfos = CountersDatabase.Instance.GetCounterNamesOld2(Id).ToDictionary(e => e.Id);
                }
            if (NameReverseInfos.ContainsKey(counterId))
                return NameReverseInfos[counterId].Name;
            return "Unknown";
        }

        public int GetNextCounterNameId()
        {
            return Interlocked.Increment(ref _maxCounterId);
        }

       
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

        private void CreateNew(string cn)
        {
            CounterNameInfoOld counterNameInfo = new CounterNameInfoOld(cn, GetNextCounterNameId(), Id);
            NameInfos.Add(cn, counterNameInfo);
            CountersDatabase.Instance.SaveCounterNameInfo(this, counterNameInfo);
        }

        private void Reload()
        {
            List<CounterNameInfo> counterCategoryInfos = CountersDatabase.Instance.GetCounterNamesOld2(Id).ToList();
            NameInfos = counterCategoryInfos.ToDictionary(e => e.Name);
            //NameInfos = counterCategoryInfos.ToDictionary(e => e.Id);
        }*/

       
    }
}