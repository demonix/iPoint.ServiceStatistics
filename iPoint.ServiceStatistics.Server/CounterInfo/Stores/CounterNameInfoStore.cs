using System.Collections.Generic;
using System.Linq;
using System.Threading;
using iPoint.ServiceStatistics.Server.DataLayer;

namespace iPoint.ServiceStatistics.Server.CounterInfo.Stores
{
    public class CounterNameInfoStore
    {
        private Dictionary<string, CounterNameInfoOld> _nameInfos;
        private Dictionary<int, CounterNameInfoOld> _nameReverseInfos;
        private int _maxCounterId;

        public CounterNameInfoStore()
        {
            _nameInfos = new Dictionary<string, CounterNameInfoOld>();
            _nameReverseInfos = new Dictionary<int, CounterNameInfoOld>();
            _maxCounterId = 0;
        }

        public CounterNameInfoOld GetById(int categoryId)
        {
            if (_nameReverseInfos.ContainsKey(categoryId))
                return _nameReverseInfos[categoryId];
            lock (_nameReverseInfos)
            {
                _nameReverseInfos = CountersDatabase.Instance.GetCounterNames2().ToDictionary(e => e.Id);
            }
            if (_nameReverseInfos.ContainsKey(categoryId))
                return _nameReverseInfos[categoryId];
            return null;
        }

        public CounterNameInfoOld GetByName(string cc)
        {
            if (_nameInfos.ContainsKey(cc))
                return _nameInfos[cc];
            lock (_nameInfos)
            {
                Reload();
                if (_nameInfos.ContainsKey(cc))
                    return _nameInfos[cc];
                CreateNew(cc);
                return _nameInfos[cc];
            }
        }

        private void CreateNew(string cn)
        {
            CounterNameInfoOld counterCategoryInfo = new CounterNameInfoOld(cn, GetNextCounterId(),);
            _catInfo.Add(cc, counterCategoryInfo);
            CountersDatabase.Instance.SaveCounterCategoryInfo(counterCategoryInfo);
        }

        private void Reload()
        {
            List<CounterCategoryInfoOld> counterCategoryInfos = CountersDatabase.Instance.GetCounterCategories2().ToList();
            _catInfo = counterCategoryInfos.ToDictionary(e => e.Name);
            //_catReverseInfo = counterCategoryInfos.ToDictionary(e => e.Id);
        }

        private int GetNextCounterId()
        {
            int nextCatId = Interlocked.Increment(ref _maxCounterId);
            return nextCatId;
        }
        
    }
}