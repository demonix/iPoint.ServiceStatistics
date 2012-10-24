using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AggregationEx;
using AggregationEx.AggregationOperations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace DataLayerEx
{
    public class Database
    {
        private static object _locker = new object();
        private MongoServer _server;
        
        private Database(MongoServer server, MongoDatabase database)
        {
            _server = server;
            MongoDb = database;
            BsonClassMap.RegisterClassMap<StoredCounter>(cm =>
            {
                cm.AutoMap();
                cm.GetMemberMap(c => c.Props)
                    .SetDefaultValue(new Dictionary<string,string>())
                    .SetIgnoreIfDefault(true);
            });
        }

        public MongoDatabase MongoDb { get; private set; }


        public static Database Instance { get; private set; }

        public IEnumerable<AggregatedValue> Find(DateTime startDate, DateTime endDate, Dictionary<string, List<string>> props)
        {
            MongoCollection<StoredCounter> items = MongoDb.GetCollection<StoredCounter>("countersData");
            IMongoQuery q = Query.And(Query.GTE("date", startDate), Query.LT("date", endDate));
            FieldsBuilder fields = new FieldsBuilder();
            //fields.Include("date", "data.Max", "data.Min", "data.Count", "data.Avg", "data.Sum");
            fields.Include("date", "data");
            

            foreach (KeyValuePair<string, List<string>> prop in props)
            {
                string fieldName = "props." + prop.Key;
                fields.Include(fieldName);
                if (prop.Value.Count == 0)
                    q = Query.And(q, Query.Exists(fieldName));
                else if (prop.Value.Count == 1)
                    q = Query.And(q, Query.EQ(fieldName, prop.Value.First()));
                else
                    q = Query.And(q, Query.In(fieldName, prop.Value.Cast<BsonString>()));
                
            }
            
            MongoCursor cursor = items.Find(q);

            cursor.SetFields(fields);
            cursor.SetSortOrder(SortBy.Ascending("date"));
            Stopwatch sw = Stopwatch.StartNew();
            var found = cursor.Cast<object>().ToList();
            sw.Stop();
            Console.WriteLine( found.Count + " entries fetched from db in " + sw.ElapsedMilliseconds + " ms");
            sw.Restart();
            var parsed = found.Cast<StoredCounter>().Select(ParseAggregatedValue).ToList();
            sw.Stop();
            Console.WriteLine(parsed.Count + " entries parsed in " + sw.ElapsedMilliseconds + " ms");
            sw.Restart();
            var result = parsed.Compact().ToList();
             sw.Stop();
             Console.WriteLine(found.Count +  " entries compacted to " + result.Count + " in " + sw.ElapsedMilliseconds + " ms");
            return result;
        }

        private static AggregatedValue ParseAggregatedValue(StoredCounter entry)
        {
            DateTime date = entry.Date;
            Dictionary<string, string> properties = entry.Props;
            AggregationKey ak = new AggregationKey(date, properties);
            AggregatedValue val = new AggregatedValue(ak);
            if (entry.Data.Sum != null) val.AddResult(new AggregationOperationResult(AggregationType.Sum, entry.Data.Sum.Value));
            if (entry.Data.Count != null) val.AddResult(new AggregationOperationResult(AggregationType.Count, entry.Data.Count.Value));
            if (entry.Data.Min != null) val.AddResult(new AggregationOperationResult(AggregationType.Min, entry.Data.Min.Min()));
            if (entry.Data.Max != null) val.AddResult(new AggregationOperationResult(AggregationType.Max, entry.Data.Max.Max()));
            if (entry.Data.Avg != null) val.AddResult(new AggregationOperationResult(AggregationType.Avg, entry.Data.Avg.Average()));
            if (entry.Data.RawValues != null) val.AddRawValues(entry.Data.RawValues);
            return val;
        }
        private static AggregatedValue ParseAggregatedValue(BsonDocument entry)
        {
            DateTime date = entry["date"].AsDateTime;
            Dictionary<string, string> properties = !entry.Contains("props")? new Dictionary<string, string>() : 
                entry["props"].AsBsonDocument.Where(kvp => kvp.Name != "pCnt").ToDictionary(kvp => kvp.Name,
                                                                                            kvp => kvp.Value.AsString);
            AggregationKey ak = new AggregationKey(date, properties);
            AggregatedValue val = new AggregatedValue(ak);
            foreach (var dataElement in entry["data"].AsBsonDocument)
            {
                AggregationType at;
                if (AggregationTypeParser.TryParse(dataElement.Name, out at))
                {
                    switch (at)
                    {
                        case AggregationType.Sum:
                        case AggregationType.Count:
                            val.AddResult(new AggregationOperationResult(at, dataElement.Value.AsDouble));
                            break;
                        case AggregationType.Min:
                            val.AddResult(new AggregationOperationResult(at,
                                                                         dataElement.Value.AsBsonArray.Select(
                                                                             v => v.AsDouble).Min()));
                            break;
                        case AggregationType.Max:
                            val.AddResult(new AggregationOperationResult(at,
                                                                         dataElement.Value.AsBsonArray.Select(
                                                                             v => v.AsDouble).Max()));
                            break;
                        case AggregationType.Avg:
                            val.AddResult(new AggregationOperationResult(at,
                                                                         dataElement.Value.AsBsonArray.Select(
                                                                             v => v.AsDouble).Average()));
                            break;
                    }
                }
                else
                {
                    if (dataElement.Name == "Raw" && dataElement.Value.AsBsonArray.Count != 0)
                        val.AddRawValues(dataElement.Value.AsBsonArray.Select(v => v.AsDouble).ToList());
                }
            }
            return val;
        }


        public void Save (AggregatedValue val)
        {
            MongoCollection<BsonDocument> items = MongoDb.GetCollection("countersData");
            UpdateBuilder updateBuilder = new UpdateBuilder();
            if (val.Count.HasValue)
                updateBuilder.Inc("data.Count", val.Count.Value);
            if (val.Sum.HasValue)
                updateBuilder.Inc("data.Sum", val.Sum.Value);
            if (val.Min.HasValue)
                updateBuilder.Push("data.Min", val.Min.Value);
            if (val.Max.HasValue)
                updateBuilder.Push("data.Max", val.Max.Value);
            if (val.Avg.HasValue)
                updateBuilder.Push("data.Avg", val.Avg.Value);
            if (val.Percentiles!=null || val.DistributionGroups!=null)
                updateBuilder.PushAll("data.Raw", val.RawValues.Select(v =>new BsonDouble(v)));

            IMongoQuery q = Query.And(Query.EQ("date", val.Date), Query.EQ("props.pCnt", val.Props.Count));
            foreach (var prop in val.Props)
            {
                q = Query.And(q, Query.EQ("props."+prop.Key, prop.Value));
            }
            items.Update(q, updateBuilder,UpdateFlags.Upsert,SafeMode.True);
        }
        
    
        public static void InitConnection(string host, int? port, string dbName)
        {
            if (Instance == null)
                lock (_locker)
                    if (Instance == null)
                        Instance = Connect(host, port, dbName);
        }

        public static void InitConnection(string mongoUrl)
        {
            if (Instance == null)
                lock (_locker)
                    if (Instance == null)
                        Instance = Connect(mongoUrl);
        }

        public static Database Connect(string mongoUrl)
        {
            MongoUrlBuilder builder = new MongoUrlBuilder(mongoUrl);
            builder.SocketTimeout = new TimeSpan(0, 30, 0);
            //builder.Server = port.HasValue ? new MongoServerAddress(host, port.Value) : new MongoServerAddress(host);
            MongoServer server = MongoServer.Create(builder.ToServerSettings());
            server.Connect();
            MongoDatabase db = server.GetDatabase(builder.DatabaseName);
            return new Database(server, db);
        }

        public static Database Connect(string host, int? port, string dbName)
        {
            MongoConnectionStringBuilder builder = new MongoConnectionStringBuilder();
            builder.SocketTimeout = new TimeSpan(0, 30, 0);
            builder.Server = port.HasValue ? new MongoServerAddress(host, port.Value) : new MongoServerAddress(host);
            MongoServer server = MongoServer.Create(builder);
            server.Connect();
            MongoDatabase db = server.GetDatabase(dbName);
            return new Database(server, db);
        }
    }
}
