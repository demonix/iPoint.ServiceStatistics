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
            var catInfo = GetCounterCategoryInfo(counterCategoryId);
            return catInfo == null ? "Unknown" : catInfo.Name;
        }

        private CounterCategoryInfo GetCounterCategoryInfo(int counterCategoryId)
        {
            CounterCategoryInfo catInfo = _cache.FindById(counterCategoryId);
            if (catInfo == null)
            {
                UpdateCache();
                catInfo = _cache.FindById(counterCategoryId);
            }
            return catInfo;
        }

        public string GetMappedCounterName(int counterCategoryId, int counterNameId)
        {
            CounterCategoryInfo catInfo = GetCounterCategoryInfo(counterCategoryId);
            if (catInfo == null) return "Unknown";
            return catInfo.GetMappedCounterName(counterNameId);

        }

        public string GetMappedCounterInstanceName(int counterCategoryId, int counterNameId, int counterInstanceId)
        {
            CounterCategoryInfo catInfo = GetCounterCategoryInfo(counterCategoryId);
            if (catInfo == null) return "Unknown";
            var nameInfo = catInfo.GetMappedCounterNameInfo(counterNameId);
            return nameInfo == null ? "Unknown" : nameInfo.GetMappedCounterInstance(counterInstanceId);
        }

        public string GetMappedCounterExtDataName(int counterCategoryId, int counterNameId, int counterExtDataId)
        {
            CounterCategoryInfo catInfo = GetCounterCategoryInfo(counterCategoryId);
            if (catInfo == null) return "Unknown";
            var nameInfo = catInfo.GetMappedCounterNameInfo(counterNameId);
            return nameInfo == null ? "Unknown" : nameInfo.GetMappedCounterExtData(counterExtDataId);
        }

        public string GetMappedCounterSourceName(int counterCategoryId, int counterNameId, int counterSourceId)
        {
            CounterCategoryInfo catInfo = GetCounterCategoryInfo(counterCategoryId);
            if (catInfo == null) return "Unknown";
            var nameInfo = catInfo.GetMappedCounterNameInfo(counterNameId);
            return nameInfo == null ? "Unknown" : nameInfo.GetMappedCounterSource(counterSourceId);
        }
    }
}