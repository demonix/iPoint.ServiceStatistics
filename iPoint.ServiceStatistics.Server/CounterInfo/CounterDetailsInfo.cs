using System.Collections.Generic;

namespace iPoint.ServiceStatistics.Server.CounterInfo
{
    public class CounterDetailsInfo
    {
        public Dictionary<string, CounterSourceInfo> SourceInfos { get; private set; }
        public Dictionary<string, CounterInstanceInfo> InstanceInfos { get; private set; }
        public Dictionary<string, CounterExtDataInfo> ExtDataInfos { get; private set; }

        public CounterDetailsInfo()
        {

        }
    }
}