using System.Collections.Generic;
using System.Linq;
using System.Threading;
using iPoint.ServiceStatistics.Server.DataLayer;

namespace iPoint.ServiceStatistics.Server.CounterInfo.Stores
{
    public class CounterCategoryInfoStore
    {
        private Dictionary<string, CounterCategoryInfoOld> _catInfo;
        private Dictionary<int, CounterCategoryInfoOld> _catReverseInfo;
        private int _maxCatId;

        public CounterCategoryInfoStore()
        {
            _catInfo = new Dictionary<string, CounterCategoryInfoOld>();
            _catReverseInfo = new Dictionary<int, CounterCategoryInfoOld>();
            _maxCatId = 0;
        }

        public CounterCategoryInfoOld GetById(int categoryId)
        {
            if (_catReverseInfo.ContainsKey(categoryId))
                return _catReverseInfo[categoryId];
            lock (_catReverseInfo)
            {
                _catReverseInfo = CountersDatabase.Instance.GetCounterCategories2().ToDictionary(e => e.Id);
            }
            if (_catReverseInfo.ContainsKey(categoryId))
                return _catReverseInfo[categoryId];
            return null;
        }

        public CounterCategoryInfoOld GetByName(string cc)
        {
            if (_catInfo.ContainsKey(cc))
                return _catInfo[cc];
            lock (_catInfo)
            {
                Reload();
                if (_catInfo.ContainsKey(cc))
                    return _catInfo[cc];
                CreateNew(cc);
                return _catInfo[cc];
            }
        }

        private void CreateNew(string cc)
        {
            CounterCategoryInfoOld counterCategoryInfo = new CounterCategoryInfoOld(cc, GetNextCategoryId());
            _catInfo.Add(cc, counterCategoryInfo);
            CountersDatabase.Instance.SaveCounterCategoryInfo(counterCategoryInfo);
        }

        private void Reload()
        {
            List<CounterCategoryInfoOld> counterCategoryInfos = CountersDatabase.Instance.GetCounterCategories2().ToList();
            _catInfo = counterCategoryInfos.ToDictionary(e => e.Name);
            //_catReverseInfo = counterCategoryInfos.ToDictionary(e => e.Id);
        }

        private int GetNextCategoryId()
        {
            int nextCatId = Interlocked.Increment(ref _maxCatId);
            return nextCatId;
        }

    }
}