using MongoDB.Bson;

namespace iPoint.ServiceStatistics.Server.КэшСчетчиков
{
    public class Cache
    {
         CounterCategoryInfoCache _cache = new CounterCategoryInfoCache();

        public Cache()
        {
            this.Load();
        }



        public string Map(string categoryName, string counterName, string source, string instance, string extData)
        {
            CounterCategoryInfo catInfo = GetOrCreateCategoryInfo(categoryName);
            CounterNameInfo nameInfo = catInfo.GetOrCreateNameInfo(counterName);
            CounterSourceInfo sourceInfo = nameInfo.GetOrCreateSourceInfo(source);
            CounterInstanceInfo instanceInfo = nameInfo.GetOrCreateInstanceInfo(instance);
            CounterExtDataInfo extDataInfo = nameInfo.GetOrCreateExtDataInfo(extData);
            return string.Join("/",sourceInfo.Id, instanceInfo.Id,extDataInfo.Id);
        }

        private CounterCategoryInfo GetOrCreateCategoryInfo(string name)
        {
            CounterCategoryInfo catInfo = _cache.FindByName(name);
            if (catInfo == null)
            {
                this.UpdateCache();
                catInfo = _cache.FindByName(name) ?? this.CreateNew(name);
            }
            return catInfo;
        }

        private void Load()
        {
            lock (_cache)
                _cache.Load();
        }
        private CounterCategoryInfo CreateNew(string name)
        {
            lock (_cache)
                return _cache.CreateNew(name);
        }

        private void UpdateCache()
        {
            lock (_cache)
                _cache.Update();
        }

        public string GetMappedCategoryName(int counterCategoryId)
        {
            CounterCategoryInfo catInfo = _cache.FindById(counterCategoryId);
            if (catInfo == null)
            {
                UpdateCache();
                catInfo = _cache.FindById(counterCategoryId);
            }
            return catInfo == null ? "Unknown" : catInfo.Name;
        }

        public string GetMappedCounterName(int counterCategoryId, int counterNameId)
        {
            CounterCategoryInfo catInfo = _cache.FindById(counterCategoryId);
            if (catInfo == null)
            {
                UpdateCache();
                catInfo = _cache.FindById(counterCategoryId);
            }
            if (catInfo == null) 
                return "Unknown";
            return catInfo.GetMappedCounterName(counterNameId);

        }
    }
}