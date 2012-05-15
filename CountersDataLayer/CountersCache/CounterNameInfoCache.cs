using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CountersDataLayer.CountersCache
{
    public class CounterNameInfoCache : InfoCacheBase<CounterNameInfo>
    {
        /* public CounterNameInfoCache LoadOrCreateNew(string cc)
         {
             CounterNameInfo cat = CountersDatabase.Instance.GetCounterNames2(cc);
             if (cat == null)
                 cat = new CounterNameInfo(cc, Interlocked.Increment(ref _maxId));
             else
                 cat.LoadCache();

             if (_dict.TryAdd(cat.Name, cat))
                 cat = _dict[cc];
             return cat;
         }*/

        private readonly int _parentCategoryId;

        public CounterNameInfoCache(int parentCategoryId)
        {
            _parentCategoryId = parentCategoryId;
        }

        public void Load()
        {
            if (_dict.Count != 0)
                return;
            List<CounterNameInfo> names =
                CountersDatabase.Instance.GetCounterNamesInCategory(_parentCategoryId).ToList();
            if (names.Count == 0)
                return;
            _maxId = names.Max(e => e.Id);
            foreach (CounterNameInfo name in names)
            {
                name.LoadCache();
                _dict.TryAdd(name.Name, name);
                _reverseDict.TryAdd(name.Id, name);
            }
        }

        public void Update()
        {
            List<CounterNameInfo> freshNames =
                CountersDatabase.Instance.GetCounterNamesInCategory(_parentCategoryId).Where(c => c.Id > _maxId).
                    ToList();
            if (freshNames.Count == 0)
                return;
            _maxId = freshNames.Max(e => e.Id);
            foreach (CounterNameInfo name in freshNames)
            {
                name.LoadCache();
                _dict.TryAdd(name.Name, name);
                _reverseDict.TryAdd(name.Id, name);
            }
        }

        public CounterNameInfo CreateNew(string name)
        {
            CounterNameInfo nameInfo = new CounterNameInfo(name, Interlocked.Increment(ref _maxId), _parentCategoryId);
            if (_dict.TryAdd(nameInfo.Name, nameInfo))
            {
                _reverseDict.TryAdd(nameInfo.Id, nameInfo);
                CountersDatabase.Instance.SaveCounterName(_parentCategoryId, nameInfo);
            }
            return nameInfo;
        }
    }
}