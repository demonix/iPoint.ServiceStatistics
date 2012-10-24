using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CountersDataLayer;

namespace CounterDataToKonturStatExporter
{
    static class DateTimeExtensons
    {
        public static DateTime RoundTo5Minutes(this DateTime dt)
        {
            return dt.AddMilliseconds(-dt.Millisecond).AddSeconds(-dt.Second).AddMinutes(-(dt.Minute%5));
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            if (!File.Exists("state"))
                File.Create("state").Close();
            string[] states = File.ReadAllLines("state");

            Console.WriteLine("Readed "+ states.Length + " states");
            string mongoUrl = File.ReadAllText("settings\\mongoConnection");
            CountersDatabase.InitConnection(mongoUrl);
            Console.WriteLine("Connection initialized");

            for (int i = 0; i < states.Length; i++)
            {
                
                string[] state = states[i].Split('\t');
                
                string category = state[0];
                string counterName = state[1];
                string counterSource = state[2];

                string counterInstance = state[3];
                string counterExtData = state[4];
                string statName = state[6];
                Console.WriteLine("Reading data for " + statName);
               

                List<object> allSeriesData = new List<object>();
                DateTime dt = DateTime.Now;

                
                DateTime now = DateTime.Now;
                DateTime startDate = state.Length == 6 ? DateTime.Parse(state[5]) : DateTime.MinValue;

               
                CounterDataParameters parameters = new CounterDataParameters(startDate.ToString("dd.MM.yyyy HH:mm:ss"),
                                                                             DateTime.MaxValue.ToString("dd.MM.yyyy HH:mm:ss"),
                                                                             Int32.Parse(category),
                                                                             Int32.Parse(counterName),
                                                                             Int32.Parse(counterSource),
                                                                             Int32.Parse(counterInstance),
                                                                             Int32.Parse(counterExtData),
                                                                             "*");


                SqlConnection connection = new SqlConnection("Data Source=app77;Initial Catalog=KeLiteDownloads;Connect Timeout=300; Max Pool Size=1000;Integrated Security=SSPI;Application Name=stats;");
                connection.Open();
                string commandText =
                    @"IF EXISTS(select 1 from Stats where [date]=@date and [statKey]=@statKey and [extend]=@extend)
									UPDATE Stats SET [value] = @value WHERE [date]=@date and [statKey]=@statKey and [extend]=@extend
									ELSE
									INSERT INTO Stats ([date], [statKey], [extend], [value]) VALUES (@date, @statKey, @extend, @value)";
                SqlCommand command = new SqlCommand(commandText,connection);
                /*List<CounterSeriesData> result = CountersDatabase.Instance.GetCounterData(
                    startDate, DateTime.MaxValue, Int32.Parse(category),
                    Int32.Parse(counterName), Int32.Parse(counterSource), Int32.Parse(counterInstance),
                    Int32.Parse(counterExtData), new List<string> { "*" });*/

                List<CounterSeriesData> result = parameters.Sources.AsParallel().SelectMany(
                    source => parameters.Instances.AsParallel().SelectMany(
                        instance => parameters.ExtendedDatas.AsParallel().SelectMany(
                            extData =>
                            CountersDatabase.Instance.GetCounterData(parameters.BeginDate, parameters.EndDate,
                                                                        parameters.CounterCategoryId,
                                                                        parameters.CounterNameId, source.Id, instance.Id,
                                                                        extData.Id, parameters.Series)
                                        ))).ToList();
                Console.WriteLine("Data for " + statName + " readed. Total " + result.Count + " values" );

                foreach (CounterSeriesData counterSeriesData in result)
                {
                    foreach (SeriesPoint seriesPoint in counterSeriesData.Points)
                    {
                        if (!seriesPoint.Value.HasValue)
                            continue;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("date", seriesPoint.DateTime.RoundTo5Minutes().ToLocalTime());
                        command.Parameters.AddWithValue("statKey", statName);
                        command.Parameters.AddWithValue("extend", counterSeriesData.CounterSource);
                        command.Parameters.AddWithValue("value", seriesPoint.Value);
                        command.ExecuteNonQuery();
                        
                        Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                            counterSeriesData.CounterCategory, counterSeriesData.CounterName, counterSeriesData.CounterSource, counterSeriesData.CounterInstance, counterSeriesData.CounterExtData,
                            counterSeriesData.SeriesName, seriesPoint.DateTime.RoundTo5Minutes().ToLocalTime(), seriesPoint.Value);
                    }
                }
                Console.WriteLine("Data saved to SQL Server");
                states[i] = String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", category, counterName, counterSource,
                                          counterInstance, counterExtData, now, statName);
            }
            File.WriteAllLines("state",states);
            
        }

        private static void Calculate(int i)
        {
            ThreadPool.QueueUserWorkItem(c => { Console.WriteLine(i); Thread.Sleep(100); });
        }
    }
}
