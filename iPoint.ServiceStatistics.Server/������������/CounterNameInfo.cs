using System.Collections.Generic;
using iPoint.ServiceStatistics.Server.DataLayer;

namespace iPoint.ServiceStatistics.Server.КэшСчетчиков
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