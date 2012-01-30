using System;
using System.Net;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using System.Linq;
using System.Collections.Generic;
using MongoDB.Driver.Builders;
using iPoint.ServiceStatistics.Server.Aggregation;

namespace iPoint.ServiceStatistics.Server.DataLayer
{
    public class CountersDatabase
    {
        private MongoServer _server;
        private MongoDatabase _database;

        private CountersDatabase(MongoServer server, MongoDatabase database)
        {
            _server = server;
            _database = database;
        }

        public static CountersDatabase Instance { get; private set; }
        private static object _locker = new object();
        


        public static void InitConnection(string host, int? port, string dbName)
        {
            if (Instance == null)
                lock (_locker)
                    if (Instance == null)
                        Instance = Connect(host, port, dbName);
        }

        public static CountersDatabase Connect(string host, int? port, string dbName)
        {
            MongoConnectionStringBuilder builder = new MongoConnectionStringBuilder();
            builder.SocketTimeout = new TimeSpan(0, 30, 0);
            builder.Server = port.HasValue ? new MongoServerAddress(host, port.Value) : new MongoServerAddress(host);
            MongoServer server = MongoServer.Create(builder); 
            server.Connect();
            MongoDatabase db = server.GetDatabase(dbName);
            return new CountersDatabase(server, db);
        }

        public void SaveCounters2(TotalAggregationResult counters)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersData");
            IEnumerable<BsonDocument> data =
                counters.ResultGroups.Select(r =>
                                                 {
                                                     BsonDocument values =
                                                         new BsonDocument(
                                                             r.Result.Select(
                                                                 v => new BsonElement(v.Item1, GetBsonValue(v.Item2))));
                                                     return new BsonDocument
                                                                {
                                                                    {"date", counters.Date},
                                                                    {"counterCategory", counters.CounterCategory},
                                                                    {"counterName", counters.CounterName},
                                                                    {"type", counters.CounterAggregationType.ToString()},
                                                                    {"source", r.CounterGroup.Source},
                                                                    {"instance", r.CounterGroup.Instance},
                                                                    {"extendedData", r.CounterGroup.ExtendedData},
                                                                    {"value", values}
                                                                };
                                                 });
            items.InsertBatch(data);
            foreach (BsonDocument bsonDocument in data)
            {
                //Console.WriteLine(1);
                Console.WriteLine(bsonDocument.ToJson());
            }
        }

        public void SaveCounters(TotalAggregationResult counters)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersData");

            BsonDocument cData = new BsonDocument()
                                     {
                                         counters.ResultGroups.Select(
                                             r => new BsonElement(r.StorageKey,
                                                                  new BsonDocument(
                                                                      r.Result.Select(
                                                                          v =>
                                                                          new BsonElement(v.Item1, GetBsonValue(v.Item2))))))
                                     };

            BsonDocument data = new BsonDocument
                                    {
                                        {"date", counters.Date},
                                        {"counterCategory", counters.CounterCategory},
                                        {"counterName", counters.CounterName},
                                        {"type", counters.CounterAggregationType.ToString()},
                                        {"data", cData}
                                    };
            items.Insert(data);
            Console.WriteLine(data.ToJson());

        }

        private BsonValue GetBsonValue(UniversalValue item)
        {
            switch (item.Type)
            {
                case UniversalValue.UniversalClassType.Numeric:
                    return new BsonDouble(item.DoubleValue);
case UniversalValue.UniversalClassType.TimeSpan:
                    return new BsonString(item.TimespanValue.ToString());
case UniversalValue.UniversalClassType.String:
                    return new BsonString(item.StringValue);
                default:
                    throw new Exception("Unknown type " + item.Type);

            }
        }

        public IEnumerable<string> GetCounterCategories(DateTime beginDate, DateTime endDate)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersData");
            QueryComplete qb = Query.GTE("date", beginDate).LTE(endDate);
            var cursor = items.Find(qb);
            cursor.SetFields(Fields.Include("counterCategory"));
            return cursor.Select(d => d["counterCategory"].AsString).Distinct();
        }

        public IEnumerable<string> GetCounterNames(DateTime beginDate, DateTime endDate, string counterCategory)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersData");
            QueryComplete qb = Query.GTE("date", beginDate).LTE(endDate);
            QueryComplete qb2 = Query.EQ("counterCategory", counterCategory);
            var cursor = items.Find(Query.And(qb,qb2));
            cursor.SetFields(Fields.Include("counterName"));
            return cursor.Select(d => d["counterName"].AsString).Distinct();
        }

       
        public IEnumerable<CounterDetail> GetCounterDetails(DateTime beginDate, DateTime endDate, string counterCategory, string counterName)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersData");
            QueryComplete qb = Query.GTE("date", beginDate).LTE(endDate);
            QueryComplete qb2 = Query.EQ("counterCategory", counterCategory);
            QueryComplete qb3 = Query.EQ("counterName", counterName);
            var cursor = items.Find(Query.And(qb, qb2,qb3));
            cursor.SetFields(Fields.Include("source", "instance", "extendedData"));
            return cursor.Select(d =>
                                 new CounterDetail(d["source"].AsString, d["instance"].AsString,
                                                   d.Contains("extendedData")? d["extendedData"].AsString: null)
                );

        }
/*
        public BsonDocument ToCustomBsonDocument()
        {
            BsonDocument values = new BsonDocument(Result.Select(r => new BsonElement(r.Item1, r.Item2.ToString())));
            return new BsonDocument(){
                {"source",CounterGroup.Source},
                {"instance",CounterGroup.Instance},
                {"extendedData",CounterGroup.ExtendedData},
                {"value",values}
                };
        }
        public BsonDocument ToCustomBsonDocument()
        {

            IEnumerable<BsonDocument> data = ResultGroups.Select(r => r.ToCustomBsonDocument());
            return new BsonDocument
                                 {
                                     {"date", Date},
                                     {"counterCategory", CounterCategory},
                                     {"counterName", CounterName},
                                     {"sources", new BsonArray(AllSources)},
                                     {"instances", new BsonArray(AllInstances)},
                                     {"extDatas", new BsonArray(AllExtendedDatas)},
                  //                   {"preAggregatedValues", new BsonDocument()
                  //                                               {
                  //                                                   {"TSTITE", new BsonArray (TSTITE.Select(e => e.ToCustomBsonDocument()))},
                  //                                                   {"TI", new BsonArray (TI.Select(e => e.ToCustomBsonDocument()))},
                  //                                                   {"TE", new BsonArray (TE.Select(e => e.ToCustomBsonDocument()))}
                  //                                               }},
                                     {"type", AggregationType.ToString()},
                                     {"data", new BsonArray(data)}
                                 };
        }*/


        public IEnumerable<List<object>> GetCounterData(DateTime beginDate, DateTime endDate, string counterCategory, string counterName,
            string counterSource, string counterInstance, string counterExtData)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersData");
            QueryComplete qb = Query.GTE("date", beginDate).LTE(endDate);
            QueryComplete qb2 = Query.EQ("counterCategory", counterCategory);
            QueryComplete qb3 = Query.EQ("counterName", counterName);
            QueryComplete qb4 = Query.EQ("source", counterSource);
            QueryComplete qb5 = Query.EQ("instance", counterInstance);
            QueryComplete qb6 = Query.EQ("extendedData", counterExtData);
            var cursor = items.Find(Query.And(qb, qb2, qb3,qb4,qb5,qb6));
            cursor.SetFields(Fields.Include("date","value"));
            return
                cursor.Select(
                    d => new List<object>()
                             {
                              (d["date"].AsNullableDateTime.Value.Ticks/TimeSpan.TicksPerMillisecond),   
                              d["value"].AsBsonDocument["value"].ToDouble()
                             });
        }
    }

    public class CounterDetail
    {
        public string Source { get; set; }
        public string Instance { get; set; }
        public string ExtData { get; set; }

        public CounterDetail(string source, string instance, string extData)
        {
            Source = source;
            Instance = instance;
            ExtData = extData;
        }
    }
    public class CounterDataValues
    {
    }
}