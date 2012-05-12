using System;
using System.Collections.Generic;
using iPoint.ServiceStatistics.Server.DataLayer;

namespace iPoint.ServiceStatistics.Server.CountersCache
{
    public class CounterCategoryInfo
    {
        CounterNameInfoCache _cache ;

        public CounterCategoryInfo(string name, int id)
        {
            _cache = new CounterNameInfoCache(id);
            Name = name;
            Id = id;
        }

        public string Name { get; private set; }
        public int Id { get; private set; }

        public void LoadCache()
        {
            lock (_cache)
                _cache.Load();
        }

        
        public void UpdateCache()
        {
            lock (_cache)
                _cache.Update();
        }

        public CounterNameInfo CreateNew(string name)
        {
            lock (_cache)
            {
               return _cache.CreateNew(name);
            }
        }


        public CounterNameInfo GetOrCreateNameInfo(string name)
        {
            CounterNameInfo nameInfo = _cache.FindByName(name);
            if (nameInfo == null)
            {
                this.UpdateCache();
                nameInfo = _cache.FindByName(name) ?? this.CreateNew(name);
            }
            return nameInfo;
        }

        public string GetMappedCounterName(int counterNameId)
        {
           var nameInfo = GetMappedCounterNameInfo(counterNameId);
           return nameInfo == null ? "Unknown" : nameInfo.Name;
        }

        public CounterNameInfo GetMappedCounterNameInfo(int counterNameId)
        {
            CounterNameInfo nameInfo = _cache.FindById(counterNameId);
            if (nameInfo == null)
            {
                UpdateCache();
                nameInfo = _cache.FindById(counterNameId);
            }
            return nameInfo;
        }

    }
}