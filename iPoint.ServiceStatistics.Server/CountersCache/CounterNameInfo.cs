using System.Collections.Generic;
using iPoint.ServiceStatistics.Server.DataLayer;

namespace iPoint.ServiceStatistics.Server.CountersCache
{
    public class CounterNameInfo
    {
        private CounterSourceInfoCache _sourceCache;
        private CounterInstanceInfoCache _instanceCache;
        private CounterExtDataInfoCache _extDataCache;

        public CounterNameInfo(string name, int id, int parentCategoryId)
        {
            _sourceCache = new CounterSourceInfoCache(parentCategoryId, id);
            _instanceCache = new CounterInstanceInfoCache(parentCategoryId,id);
            _extDataCache = new CounterExtDataInfoCache(parentCategoryId, id);
            Name = name;
            Id = id;
        }
        
        public string Name { get; private set; }
        public int Id { get; private set; }


        public void LoadCache()
        {
            _sourceCache.Load();
            _instanceCache.Load();
            _extDataCache.Load();
        }

        public string GetMappedCounterInstance(int counterInstanceId)
        {
            var instanceInfo = GetMappedCounterInstanceInfo(counterInstanceId);
            return instanceInfo == null ? "Unknown" : instanceInfo.Name;
        }

        public CounterInstanceInfo GetMappedCounterInstanceInfo(int counterInstanceId)
        {
            CounterInstanceInfo instanceInfo = _instanceCache.FindById(counterInstanceId);
            if (instanceInfo == null)
            {
                UpdateInstanceCache();
                instanceInfo = _instanceCache.FindById(counterInstanceId);
            }
            return instanceInfo;
        }

        public string GetMappedCounterSource(int counterInstanceId)
        {
            var sourceInfo = GetMappedCounterSourceInfo(counterInstanceId);
            return sourceInfo == null ? "Unknown" : sourceInfo.Name;
        }

        public CounterSourceInfo GetMappedCounterSourceInfo(int counterInstanceId)
        {
            CounterSourceInfo sourceInfo = _sourceCache.FindById(counterInstanceId);
            if (sourceInfo == null)
            {
                UpdateSourceCache();
                sourceInfo = _sourceCache.FindById(counterInstanceId);
            }
            return sourceInfo;
        }

        public string GetMappedCounterExtData(int counterInstanceId)
        {
            var extDataInfo = GetMappedCounterExtDataInfo(counterInstanceId);
            return extDataInfo == null ? "Unknown" : extDataInfo.Name;
        }

        public CounterExtDataInfo GetMappedCounterExtDataInfo(int counterInstanceId)
        {
            CounterExtDataInfo extDataInfo = _extDataCache.FindById(counterInstanceId);
            if (extDataInfo == null)
            {
                UpdateExtDataCache();
                extDataInfo = _extDataCache.FindById(counterInstanceId);
            }
            return extDataInfo;
        }

        public CounterSourceInfo GetOrCreateSourceInfo(string name)
        {
            CounterSourceInfo sourceInfo = _sourceCache.FindByName(name);
            if (sourceInfo == null)
            {
                UpdateSourceCache();
                sourceInfo = _sourceCache.FindByName(name) ?? this.CreateNewSourceInfo(name);
            }
            return sourceInfo;
        }

        private void UpdateSourceCache()
        {
            lock (_sourceCache)
                _sourceCache.Update();
        }

        public CounterSourceInfo CreateNewSourceInfo(string name)
        {
            lock (_sourceCache)
                return _sourceCache.CreateNew(name);
        }

        public CounterInstanceInfo GetOrCreateInstanceInfo(string name)
        {
            CounterInstanceInfo instanceInfo = _instanceCache.FindByName(name);
            if (instanceInfo == null)
            {
                UpdateInstanceCache();
                instanceInfo = _instanceCache.FindByName(name) ?? this.CreateNewInstanceInfo(name);
            }
            return instanceInfo;
        }

        private void UpdateInstanceCache()
        {
            lock (_instanceCache)
                _instanceCache.Update();
        }

        private CounterInstanceInfo CreateNewInstanceInfo(string name)
        {
            lock (_instanceCache)
                return _instanceCache.CreateNew(name);
        }

        public CounterExtDataInfo GetOrCreateExtDataInfo(string name)
        {
            CounterExtDataInfo instanceInfo = _extDataCache.FindByName(name);
            if (instanceInfo == null)
            {
                UpdateExtDataCache();
                instanceInfo = _extDataCache.FindByName(name) ?? this.CreateNewExtDataInfo(name);
            }
            return instanceInfo;
        }

        private void UpdateExtDataCache()
        {
            lock (_extDataCache)
                _extDataCache.Update();
        }

        private CounterExtDataInfo CreateNewExtDataInfo(string name)
        {
            lock (_extDataCache)
                return _extDataCache.CreateNew(name);
        }
    }
}