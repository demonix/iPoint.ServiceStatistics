﻿using System;
using System.Collections.Generic;
using iPoint.ServiceStatistics.Server.DataLayer;

namespace iPoint.ServiceStatistics.Server.КэшСчетчиков
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
            CounterNameInfo catInfo = _cache.FindById(counterNameId);
            if (catInfo == null)
            {
                UpdateCache();
                catInfo = _cache.FindById(counterNameId);
            }
            return catInfo == null ? "Unknown" : catInfo.Name;
        }
    }
}