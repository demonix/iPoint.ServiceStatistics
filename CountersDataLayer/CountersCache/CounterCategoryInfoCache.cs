using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CountersDataLayer.CountersCache
{
    public class CounterCategoryInfoCache : InfoCacheBase<CounterCategoryInfo>
    {

        public void Load()
        {

            if (_dict.Count != 0)
                return;
            List<CounterCategoryInfo> categories = CountersDatabase.Instance.GetCounterCategories2().ToList();
            if (categories.Count == 0)
                return;
            _maxId = categories.Max(e => e.Id);
            foreach (CounterCategoryInfo cat in categories)
            {
                cat.LoadCache();
                _dict.TryAdd(cat.Name, cat);
                _reverseDict.TryAdd(cat.Id, cat);
            }
        }

        public void Update()
        {
            List<CounterCategoryInfo> freshCategories =
                CountersDatabase.Instance.New_GetCounterCategories().Where(c => c.Id > _maxId).ToList();
            if (freshCategories.Count == 0)
                return;
            _maxId = freshCategories.Max(e => e.Id);
            foreach (CounterCategoryInfo cat in freshCategories)
            {
                cat.LoadCache();
                _dict.TryAdd(cat.Name, cat);
                _reverseDict.TryAdd(cat.Id, cat);
            }

        }

        public CounterCategoryInfo CreateNew(string name)
        {
            CounterCategoryInfo cat = new CounterCategoryInfo(name, Interlocked.Increment(ref _maxId));
            if (_dict.TryAdd(cat.Name, cat))
            {
                _reverseDict.TryAdd(cat.Id, cat);
                CountersDatabase.Instance.New_SaveCounterCategory(cat);
            }
            return cat;
        }
    }
}