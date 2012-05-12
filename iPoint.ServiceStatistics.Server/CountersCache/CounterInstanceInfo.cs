using System.Collections.Generic;
using System.Linq;
using System.Threading;
using iPoint.ServiceStatistics.Server.DataLayer;

namespace iPoint.ServiceStatistics.Server.CountersCache
{
    public class CounterInstanceInfo
    {
        public CounterInstanceInfo(string name, int id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; private set; }
        public int Id { get; private set; }
    }
}