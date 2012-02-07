using System;
using System.Collections.Generic;
using System.IO;
using iPoint.ServiceStatistics.Server.КэшСчетчиков;


namespace iPoint.ServiceStatistics.Server
{
    public class Settings
    {
        public static Cache CountersMapper { get; private set; }

        public static ExtendedDataTransformations ExtendedDataTransformations  = new ExtendedDataTransformations();

        public List<AggregationParameters> AggregationParameters { get; private set; }

        public Settings()
        {
            AggregationParameters = new List<AggregationParameters>();
            if (File.Exists(@"settings\counters.list"))
            {
                string[] counters = File.ReadAllLines(@"settings\counters.list");
                foreach (string counter in counters)
                {
                    string[] paramerers = counter.Split('\t');
                    try
                    {
                        AggregationType at;
                        Enum.TryParse(paramerers[2], out at);
                        AggregationParameters.Add(new AggregationParameters(paramerers[0], paramerers[1], at,
                                                                            System.Type.GetType(paramerers[3])));
                    }
                    catch{}
                }
                /*AggregationParameters.Add(new AggregationParameters("FT", "RN_Sent", AggregationType.Sum,
                                                                    System.Type.GetType("System.Int32")));
                AggregationParameters.Add(new AggregationParameters("PrintServer", "IncomingRequestCount",
                                                                    AggregationType.Sum,
                                                                    System.Type.GetType("System.Int32")));
                AggregationParameters.Add(new AggregationParameters("ServiceInteraction", "RequestProcessingTimes",
                                                                    AggregationType.Percentile,
                                                                    System.Type.GetType("System.TimeSpan")));
                AggregationParameters.Add(new AggregationParameters("ServiceInteraction", "Graylisting",
                                                                    AggregationType.Sum,
                                                                    System.Type.GetType("System.Int32")));*/
                //AggregationParameters.Add(new AggregationParameters("ServiceInteraction", "RequestProcessingTimes_print_servers", AggregationType.Percentile, System.Type.GetType("System.TimeSpan")));
                //AggregationParameters.Add(new AggregationParameters("ServiceInteraction", "RequestProcessingTimes_print_servers", AggregationType.Percentile, System.Type.GetType("System.TimeSpan")));
            }
            CountersMapper = new Cache();
            //ExtendedDataTransformations = new ExtendedDataTransformations();
        }
    }
}