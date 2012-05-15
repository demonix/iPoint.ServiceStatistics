using System.Collections.Concurrent;

namespace CountersDataLayer.CountersCache
{
    public class InfoCacheBase<T> where T:class
    {
        internal ConcurrentDictionary<string, T> _dict;
        internal  ConcurrentDictionary<int, T> _reverseDict;
        internal  int _maxId;
        
        public InfoCacheBase()
        {
            _dict = new ConcurrentDictionary<string, T>();
            _reverseDict = new ConcurrentDictionary<int, T>();
            _maxId = 0;
        }

        public T FindByName(string name)
        {
            if (_dict.ContainsKey(name))
                return _dict[name];
            return null;
        }

        public T FindById(int id)
        {
            if (_reverseDict.ContainsKey(id))
                return _reverseDict[id];
            return null;
        }


       
    }
}