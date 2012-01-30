using System.Collections.Generic;

namespace iPoint.ServiceStatistics.Server
{
    public class Settings
    {
        public List<AggregationParameters> AggregationParameters { get; private set; }

        public Settings()
        {
            AggregationParameters = new List<AggregationParameters>();
            AggregationParameters.Add(new AggregationParameters("FT","RN_Sent",AggregationType.Sum,System.Type.GetType("System.Int32")));
            AggregationParameters.Add(new AggregationParameters("PrintServer","IncomingRequestCount",AggregationType.Sum,System.Type.GetType("System.Int32")));
        }
    }
}