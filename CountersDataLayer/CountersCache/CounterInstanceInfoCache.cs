using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CountersDataLayer.CountersCache
{
    public class CounterInstanceInfoCache : InfoCacheBase<CounterInstanceInfo>
    {
         private readonly int _parentCategoryId;
        private readonly int _parentCounterId;

        public CounterInstanceInfoCache(int parentCategoryId, int parentCounterId)
        {
            _parentCategoryId = parentCategoryId;
            _parentCounterId = parentCounterId;
        }
        public CounterInstanceInfo CreateNew(string name)
        {
            CounterInstanceInfo instanceInfo = new CounterInstanceInfo(name, Interlocked.Increment(ref _maxId));
            if (_dict.TryAdd(instanceInfo.Name, instanceInfo))
            {
                _reverseDict.TryAdd(instanceInfo.Id, instanceInfo);
                CountersDatabase.Instance.SaveCounterInstance(_parentCategoryId, _parentCounterId, instanceInfo);
            }
            return instanceInfo;
        }

        public void Load()
        {
            if (_dict.Count != 0)
                return;
            List<CounterInstanceInfo> instances =
                CountersDatabase.Instance.GetCounterInstances(_parentCategoryId, _parentCounterId).ToList();
            if (instances.Count == 0)
                return;
            _maxId = instances.Max(e => e.Id);
            foreach (CounterInstanceInfo instance in instances)
            {
                _dict.TryAdd(instance.Name, instance);
                _reverseDict.TryAdd(instance.Id, instance);
            }
        }

        public void Update()
        {
            List<CounterInstanceInfo> instances =
                CountersDatabase.Instance.GetCounterInstances(_parentCategoryId, _parentCounterId).Where(e=>e.Id>_maxId).ToList();
            if (instances.Count == 0)
                return;
            _maxId = instances.Max(e => e.Id);
            foreach (CounterInstanceInfo instance in instances)
            {
                _dict.TryAdd(instance.Name, instance);
                _reverseDict.TryAdd(instance.Id, instance);
            }
        }
    }
}