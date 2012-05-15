using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CountersDataLayer.CountersCache
{
    public class CounterSourceInfoCache : InfoCacheBase<CounterSourceInfo>
    {
        private readonly int _parentCategoryId;
        private readonly int _parentCounterId;

        public CounterSourceInfoCache(int parentCategoryId, int parentCounterId)
        {
            _parentCategoryId = parentCategoryId;
            _parentCounterId = parentCounterId;
        }

        public CounterSourceInfo CreateNew(string name)
        {
            CounterSourceInfo sourceInfo = new CounterSourceInfo(name, Interlocked.Increment(ref _maxId));
            if (_dict.TryAdd(sourceInfo.Name, sourceInfo))
            {
                _reverseDict.TryAdd(sourceInfo.Id, sourceInfo);
                CountersDatabase.Instance.New_SaveCounterSource(_parentCategoryId, _parentCounterId, sourceInfo);
            }
            return sourceInfo;
        }

        public void Load()
        {
            if (_dict.Count != 0)
                return;
            List<CounterSourceInfo> sources =
                CountersDatabase.Instance.New_GetCounterSources(_parentCategoryId, _parentCounterId).ToList();
            if (sources.Count == 0)
                return;
            _maxId = sources.Max(e => e.Id);
            foreach (CounterSourceInfo source in sources)
            {
                _dict.TryAdd(source.Name, source);
                _reverseDict.TryAdd(source.Id, source);
            }
        }

        public void Update()
        {
            List<CounterSourceInfo> freshSources = CountersDatabase.Instance.New_GetCounterSources(_parentCategoryId, _parentCounterId).Where(c => c.Id > _maxId).
                    ToList();
            if (freshSources.Count == 0)
                return;
            _maxId = freshSources.Max(e => e.Id);
            foreach (CounterSourceInfo source in freshSources)
            {
                _dict.TryAdd(source.Name, source);
                _reverseDict.TryAdd(source.Id, source);
            }
        }
    }
}