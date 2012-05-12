using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Subjects;
using EventEvaluationLib;
using iPoint.ServiceStatistics.Server.КэшСчетчиков;


namespace iPoint.ServiceStatistics.Server
{
    public class Settings
    {
        public static Cache CountersMapper { get; private set; }

        public static ExtendedDataTransformations ExtendedDataTransformations  = new ExtendedDataTransformations();
        public IConnectableObservable<IList<LogEventArgs>> ObservableEvents;

        //public List<AggregationParameters> AggregationParameters { get; private set; }
        public List<CounterAggregator> Aggregators { get; private set; }

        public Settings()
        {
            Aggregators = new List<CounterAggregator>();
          //  AggregationParameters = new List<AggregationParameters>();
            CountersMapper = new Cache();
            //ExtendedDataTransformations = new ExtendedDataTransformations();
        }

        public void ReadAggregators()
        {
            if (!File.Exists(@"settings\counters.list")) return;
            string[] counters = File.ReadAllLines(@"settings\counters.list");
            
            foreach (string counter in counters)
            {
                string[] paramerers = counter.Split('\t');
                try
                {
                    AggregationType at;
                    Enum.TryParse(paramerers[2], out at);

                    AddAggregator(new CounterAggregator(paramerers[0], paramerers[1], at, System.Type.GetType(paramerers[3]), paramerers.Length>4?paramerers[4]:""));
                }
                catch (Exception ex)
                { Console.WriteLine(ex); }
            }
        }

        public void AddAggregator(CounterAggregator aggregator)
        {
            lock (Aggregators)
            {
                if (!Aggregators.Contains(aggregator))
                {
                    aggregator.BeginAggregation(ObservableEvents);
                    Aggregators.Add(aggregator);
                }
            }
            
        }
    }
}