using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using iPoint.ServiceStatistics.Server.CounterInfo;
using iPoint.ServiceStatistics.Server.CounterInfo.Stores;
using iPoint.ServiceStatistics.Server.DataLayer;

namespace iPoint.ServiceStatistics.Server
{
    public class CounterMapper
    {
        private CounterCategoryInfoStore _store;
        
        public CounterMapper()
        {
            _store = new CounterCategoryInfoStore();
        }

        public string Map(string cc, string cn, string cs, string ci, string ced)
        {
            CounterCategoryInfoOld counterCategory = _store.GetByName(cc);
            CounterNameInfoOld counter = counterCategory.GetCounter(cn);
            CounterSourceInfo counterSource = counter.GetSource(cs);
            CounterInstanceInfo counterInstance = GetInstance( counterCategory, counter, ci);
            CounterExtDataInfo counterExtData = GetExtData(counterCategory, counter, ced);
            return String.Join("|", "s"+counterSource.Id, "i"+counterInstance.Id, "ed"+counterExtData.Id);
        }

        private CounterCategoryInfoOld GetByName(string cc)
        {
            throw new NotImplementedException();
        }

        /*

        public string GetMappedCounterName (int categoryId, int counterId)
        {
            if (_catReverseInfo.ContainsKey(categoryId))
                return _catReverseInfo[categoryId].GetMappedCounterName(counterId);
            lock (_catReverseInfo)
            {
                _catReverseInfo = CountersDatabase.Instance.GetCounterCategoriesOld2().ToDictionary(e => e.Id);
            }
            if (_catReverseInfo.ContainsKey(categoryId))
                return _catReverseInfo[categoryId].GetMappedCounterName(counterId);
            return "Unknown";
        }
        
        public string Map(CountersDatabase cDb, string cc, string cn, string cs, string ci, string ced)
        {
            CounterCategoryInfo counterCategory = _store.GetByName(cc);
            CounterNameInfoOld counter = counterCategory.GetCounter(cn);
            CounterSourceInfo counterSource = counter.GetSource(cs);
            CounterInstanceInfo counterInstance = GetInstance( counterCategory, counter, ci);
            CounterExtDataInfo counterExtData = GetExtData(counterCategory, counter, ced);
            return String.Join("|", "s"+counterSource.Id, "i"+counterInstance.Id, "ed"+counterExtData.Id);
        }

        private CounterExtDataInfo GetExtData(CounterCategoryInfo counterCategory, CounterNameInfoOld counter, string ced)
        {
            lock (counter)
            {
                if (!counter.ExtDataInfos.ContainsKey(ced))
                {
                    CounterExtDataInfo info = new CounterExtDataInfo(ced, counter.GetNextExtDataId());
                    counter.ExtDataInfos.Add(ced, info);
                    CountersDatabase.Instance.SaveCounterDetailsInfo(counterCategory, counter, null, null, info);
                    return info;
                }
            }
            return counter.ExtDataInfos[ced];
        }

        private CounterInstanceInfo GetInstance( CounterCategoryInfo counterCategory, CounterNameInfoOld counter, string ci)
        {
            lock (counter)
            {
                if (!counter.InstanceInfos.ContainsKey(ci))
                {
                    CounterInstanceInfo info = new CounterInstanceInfo(ci, counter.GetNextInstanceId());
                    counter.InstanceInfos.Add(ci, info);
                    CountersDatabase.Instance.SaveCounterDetailsInfo(counterCategory, counter, null, info, null);
                    return info;
                }
            }
            return counter.InstanceInfos[ci];
        }

        private CounterSourceInfo GetSource(CounterCategoryInfo counterCategory, CounterNameInfoOld counter, string cs)
        {
            lock (counter)
            {
                if (!counter.SourceInfos.ContainsKey(cs))
                {
                    CounterSourceInfo info = new CounterSourceInfo(cs, counter.GetNextSourceId());
                    counter.SourceInfos.Add(cs, info);
                    CountersDatabase.Instance.SaveCounterDetailsInfo(counterCategory, counter, info, null, null);
                    return info;
                }
            }
            return counter.SourceInfos[cs];
        }

       */

        
    }
}