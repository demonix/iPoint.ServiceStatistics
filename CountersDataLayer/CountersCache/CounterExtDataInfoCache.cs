using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CountersDataLayer.CountersCache
{
    public class CounterExtDataInfoCache : InfoCacheBase<CounterExtDataInfo>
    {
         private readonly int _parentCategoryId;
        private readonly int _parentCounterId;

        public CounterExtDataInfoCache(int parentCategoryId, int parentCounterId)
        {
            _parentCategoryId = parentCategoryId;
            _parentCounterId = parentCounterId;
        }

        public CounterExtDataInfo CreateNew(string name)
        {
            CounterExtDataInfo extDataInfo = new CounterExtDataInfo(name, Interlocked.Increment(ref _maxId));
            if (_dict.TryAdd(extDataInfo.Name, extDataInfo))
            {
                _reverseDict.TryAdd(extDataInfo.Id, extDataInfo);
                CountersDatabase.Instance.New_SaveCounterExtData(_parentCategoryId, _parentCounterId, extDataInfo);
            }
            return extDataInfo;
        }

        public void Load()
        {
            if (_dict.Count != 0)
                return;
            List<CounterExtDataInfo> extDatas =
                CountersDatabase.Instance.New_GetCounterExtDatas(_parentCategoryId, _parentCounterId).ToList();
            if (extDatas.Count == 0)
                return;
            _maxId = extDatas.Max(e => e.Id);
            foreach (CounterExtDataInfo extData in extDatas)
            {
                _dict.TryAdd(extData.Name, extData);
                _reverseDict.TryAdd(extData.Id, extData);
            }
        }

        public void Update()
        {
            List<CounterExtDataInfo> extDatas =
                CountersDatabase.Instance.New_GetCounterExtDatas(_parentCategoryId, _parentCounterId).Where(e=>e.Id>_maxId).ToList();
            if (extDatas.Count == 0)
                return;
            _maxId = extDatas.Max(e => e.Id);
            foreach (CounterExtDataInfo extData in extDatas)
            {
                _dict.TryAdd(extData.Name, extData);
                _reverseDict.TryAdd(extData.Id, extData);
            }
        }
    }
}