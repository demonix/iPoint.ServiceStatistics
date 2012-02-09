using System;
using System.Dynamic;
using System.Net;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using System.Linq;
using System.Collections.Generic;
using MongoDB.Driver.Builders;
using iPoint.ServiceStatistics.Server.Aggregation;
using iPoint.ServiceStatistics.Server.ÊýøÑ÷åò÷èêîâ;

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

        /*public void SaveCounters2(TotalAggregationResult counters)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersData");
            IEnumerable<BsonDocument> data =
                counters.ResultGroups.Select(r =>
                                                 {
                                                     BsonDocument values = new BsonDocument(r.Result.Select(v => new BsonElement(v.Item1, GetBsonValue(v.Item2))));
                                                     BsonDocument counter = new BsonDocument
                                                                                {
                                                                                    {"category",counters.CounterCategory},
                                                                                    {"name",counters.CounterCategory},
                                                                                    {"instance",counters.CounterCategory},
                                                                                    {"source",counters.CounterCategory},
                                                                                    {"extData",counters.CounterCategory}
                                                                                };
                                                     return new BsonDocument
                                                                {
                                                                    {"date", counters.Date},
                                                                    {"counter", counter},
                                                                    {"type", counters.CounterAggregationType.ToString()},
                                                                    {"data", values}
                                                                };
                                                 });
            
            items.InsertBatch(data);
            MongoCollection<BsonDocument> countersInfo = _database.GetCollection("countersInfo");

            foreach (BsonDocument bsonDocument in data)
            {
                //Console.WriteLine(1);
                Console.WriteLine(bsonDocument.ToJson());
            }
        }*/

      

        public void SaveCounters(TotalAggregationResult counters)
        {
     
            MongoCollection<BsonDocument> items = _database.GetCollection("countersData");

            BsonDocument cData = new BsonDocument()
                                     {
                                         counters.ResultGroups.Select(
                                             r =>
                                             new BsonElement(Settings.CountersMapper.Map(counters.CounterCategory,
                                                                             counters.CounterName, 
                                                                             r.CounterGroup.Source,
                                                                             r.CounterGroup.Instance,
                                                                             r.CounterGroup.ExtendedData),
                                                 new BsonDocument(r.Result.Select(v => new BsonElement(v.Item1, GetBsonValue(v.Item2))))))
                                     };

            BsonDocument data = new BsonDocument
                                    {
                                        {"date", counters.Date},
                                        {"counterCategory", counters.CounterCategory},
                                        {"counterName", counters.CounterName},
                                        {"type", counters.CounterAggregationType.ToString()},
                                        {"data", cData}
                                    };
            Console.WriteLine(cData.ElementCount + " combinations of " + counters.CounterCategory + "." + counters.CounterName + " aggregated for " + DateTime.Now.Subtract(counters.Date).TotalMilliseconds / 1000d + " seconds. (" + counters.Date.TimeOfDay+")");
            items.Insert(data);
            

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

     /*   public void SaveCounterCategoryInfo(CounterCategoryInfoOld catInfo)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            items.Insert(new BsonDocument
                             {
                                 {"category", catInfo.Name},
                                 {"id", catInfo.Id},
                             }, SafeMode.True);
        }*/

       /* public void SaveCounterNameInfo(CounterCategoryInfoOld catInfo, CounterNameInfo nameInfo)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            IMongoQuery q = Query.EQ("category", catInfo.Name);
            UpdateBuilder  u = new UpdateBuilder();
            u.AddToSet("counters", new BsonDocument{{"name",nameInfo.Name}, {"id",nameInfo.Id}});
            items.Update(q, u, UpdateFlags.Upsert);
        }*/

        /*public void SaveCounterDetailsInfo(CounterCategoryInfoOld catInfo, CounterNameInfo nameInfo, CounterSourceInfo counterSourceInfo, CounterInstanceInfo counterInstanceInfo, CounterExtDataInfo counterExtDataInfo)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            IMongoQuery q = Query.And(Query.EQ("id", catInfo.Id));
            UpdateBuilder u = new UpdateBuilder();
            if (counterSourceInfo != null)
                u.AddToSet("c" + nameInfo.Id + ".sources",
                           new BsonDocument {{"name", counterSourceInfo.Name}, {"id", counterSourceInfo.Id}});
            if (counterInstanceInfo != null)
                u.AddToSet("c" + nameInfo.Id + ".instances",
                           new BsonDocument {{"name", counterInstanceInfo.Name}, {"id", counterInstanceInfo.Id}});
            if (counterExtDataInfo != null)
                u.AddToSet("c" + nameInfo.Id + ".extDatas",
                           new BsonDocument {{"name", counterExtDataInfo.Name}, {"id", counterExtDataInfo.Id}});
            items.Update(q, u, UpdateFlags.Upsert);
        }
*/

       /* public IEnumerable<CounterCategoryInfoOld> GetCounterCategoriesOld2()
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            var cursor = items.FindAll();
            cursor.SetFields(Fields.Include("category","id"));
            return cursor.Select(d => new CounterCategoryInfoOld(d["category"].AsString, d["id"].AsInt32));
        }*/

        public IEnumerable<CounterCategoryInfo> GetCounterCategories2()
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            var cursor = items.FindAll();
            cursor.SetFields(Fields.Include("category", "id"));
            return cursor.Select(d => new CounterCategoryInfo(d["category"].AsString, d["id"].AsInt32));
        }

        public CounterCategoryInfo GetCounterCategory2(string categoryName)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            QueryComplete q = Query.EQ("category", categoryName);
            var cursor = items.FindAll();
            cursor.SetFields(Fields.Include("category", "id"));
            var res = cursor.FirstOrDefault();
            return res == null ? null : new CounterCategoryInfo(res["category"].AsString, res["id"].AsInt32);
        }

        /*public IEnumerable<CounterNameInfoOld> GetCounterNamesOld2(int categoryId)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            QueryComplete q = Query.EQ("id", categoryId);
            var cursor = items.Find(q);
            cursor.SetFields(Fields.Include("counters"));
            var counter = cursor.FirstOrDefault();
            if (counter == null || counter["counters"] ==null)
                return Enumerable.Empty<CounterNameInfoOld>();
            return counter["counters"].AsBsonArray.Select(d => new CounterNameInfoOld(d.AsBsonDocument["name"].AsString, d.AsBsonDocument["id"].AsInt32, categoryId));
        }*/

        public IEnumerable<CounterNameInfo> GetCounterNames2(int categoryId)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            QueryComplete q = Query.EQ("id", categoryId);
            var cursor = items.Find(q);
            cursor.SetFields(Fields.Include("counters"));
            var counter = cursor.FirstOrDefault();
            if (counter == null || counter["counters"] == null)
                return Enumerable.Empty<CounterNameInfo>();
            return counter["counters"].AsBsonArray.Select(d => new CounterNameInfo(d.AsBsonDocument["name"].AsString, d.AsBsonDocument["id"].AsInt32, categoryId));
        }

        public IEnumerable<CounterSourceInfo> New_GetCounterSources(int categoryId, int counterId)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            QueryComplete q = Query.EQ("id", categoryId);
            var cursor = items.Find(q);
            cursor.SetFields(Fields.Include("c" + counterId + ".sources"));
            var counter = cursor.FirstOrDefault();
            if (counter == null || counter["c" + counterId] == null || counter["c" + counterId].AsBsonDocument["sources"]==null)
                return Enumerable.Empty<CounterSourceInfo>();
            return counter["c" + counterId].AsBsonDocument["sources"].AsBsonArray.Select(d => new CounterSourceInfo(d.AsBsonDocument["name"].AsString, d.AsBsonDocument["id"].AsInt32));
        }

        public IEnumerable<CounterInstanceInfo> New_GetCounterInstances(int categoryId, int counterId)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            QueryComplete q = Query.EQ("id", categoryId);
            var cursor = items.Find(q);
            cursor.SetFields(Fields.Include("c" + counterId + ".instances"));
            var counter = cursor.FirstOrDefault();
            if (counter == null || counter["c" + counterId] == null || counter["c" + counterId].AsBsonDocument["instances"] == null)
                return Enumerable.Empty<CounterInstanceInfo>();
            return counter["c" + counterId].AsBsonDocument["instances"].AsBsonArray.Select(d => new CounterInstanceInfo(d.AsBsonDocument["name"].AsString, d.AsBsonDocument["id"].AsInt32));
        }

        public IEnumerable<CounterExtDataInfo> New_GetCounterExtDatas(int categoryId, int counterId)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            QueryComplete q = Query.EQ("id", categoryId);
            var cursor = items.Find(q);
            cursor.SetFields(Fields.Include("c" + counterId + ".extDatas"));
            var counter = cursor.FirstOrDefault();
            if (counter == null || counter["c" + counterId] == null || counter["c" + counterId].AsBsonDocument["extDatas"] == null)
                return Enumerable.Empty<CounterExtDataInfo>();
            return counter["c" + counterId].AsBsonDocument["extDatas"].AsBsonArray.Select(d => new CounterExtDataInfo(d.AsBsonDocument["name"].AsString, d.AsBsonDocument["id"].AsInt32));
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
                              (d["date"].AsNullableDateTime.Value.Ticks/TimeSpan.TicksPerMillisecond), d["value"].AsBsonDocument["value"].ToDouble()
                             });
        }

        public Dictionary<string, List<List<object>>> GetCounterData2(DateTime beginDate, DateTime endDate, int counterCategoryId, int counterNameId,
           int counterSourceId, int counterInstanceId, int counterExtDataId)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersData");
            QueryComplete qb = Query.GTE("date", beginDate).LTE(endDate);
            QueryComplete qb2 = Query.EQ("counterCategory", Settings.CountersMapper.GetMappedCategoryName(counterCategoryId));
            QueryComplete qb3 = Query.EQ("counterName", Settings.CountersMapper.GetMappedCounterName(counterCategoryId,counterNameId));
            SortByBuilder sort = new SortByBuilder();
            sort.Ascending("date");
            var cursor = items.Find(Query.And(qb, qb2, qb3));
            cursor.SetSortOrder(sort);
            string key = counterSourceId + "/" + counterInstanceId + "/" + counterExtDataId;
            cursor.SetFields(Fields.Include("type","date", "data." + key));
            Dictionary<string, List<List<object>>> result = new Dictionary<string, List<List<object>>>();
            foreach (BsonDocument cnt in cursor)
            {
                var data = cnt["data"].AsBsonDocument;
                
                if (!data.Contains(key))
                {
                    foreach (string dicKey in result.Keys)
                    {
                        result[dicKey].Add(new List<object>
                                               {
                                                   cnt["date"].AsDateTime.ToLocalTime().Ticks/TimeSpan.TicksPerMillisecond,
                                                   "null"
                                               });

                    }
                    continue;
                }
                var values = data[key].AsBsonDocument;
                
                foreach (BsonElement bsonElement in values)
                {
                    if (!result.ContainsKey(bsonElement.Name))
                        result.Add(bsonElement.Name, new List<List<object>>());

                    result[bsonElement.Name].Add(
                        new List<object>
                            {
                                cnt["date"].AsDateTime.ToLocalTime().Ticks/TimeSpan.TicksPerMillisecond,
                                bsonElement.Value.IsString? TimeSpan.Parse(bsonElement.Value.AsString).TotalMilliseconds: bsonElement.Value.ToDouble()
                            });
                }

            }


            return result;
        }

        
       
        public IEnumerable<CounterNameInfo> New_GetCounterNamesInCategory(int categoryId)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            QueryComplete q = Query.EQ("id", categoryId);
            var cursor = items.Find(q);
            cursor.SetFields(Fields.Include("counters"));
            var counter = cursor.FirstOrDefault();
            if (counter == null || counter["counters"] == null)
                return Enumerable.Empty<CounterNameInfo>();
            return counter["counters"].AsBsonArray.Select(d => new CounterNameInfo(d.AsBsonDocument["name"].AsString, d.AsBsonDocument["id"].AsInt32,categoryId));
        }

        
        public void New_SaveCounterCategory(CounterCategoryInfo cat)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            items.Insert(new BsonDocument
                             {
                                 {"category", cat.Name},
                                 {"id", cat.Id},
                                 {"counters", new BsonArray()}
                             }, SafeMode.True);
        }

        public void New_SaveCounterName(int parentCategoryId, CounterNameInfo nameInfo)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            IMongoQuery q = Query.EQ("id", parentCategoryId);
            UpdateBuilder u = new UpdateBuilder();
            u.AddToSet("counters", new BsonDocument { { "name", nameInfo.Name }, { "id", nameInfo.Id } });
            u.Set("c" + nameInfo.Id, new BsonDocument
                                         {
                                             {"sources", new BsonArray()},
                                             {"instances", new BsonArray()},
                                             {"extDatas", new BsonArray()}
                                         });
            items.Update(q, u, UpdateFlags.Upsert,SafeMode.True);
        }

        public IEnumerable<CounterCategoryInfo> New_GetCounterCategories()
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            var cursor = items.FindAll();
            cursor.SetFields(Fields.Include("category", "id"));
            return cursor.Select(d => new CounterCategoryInfo(d["category"].AsString, d["id"].AsInt32));
        }

        public void New_SaveCounterSource(int parentCategoryId, int parentCounterId, CounterSourceInfo counterSourceInfo)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            IMongoQuery q = Query.And(Query.EQ("id", parentCategoryId));
            UpdateBuilder u = new UpdateBuilder();
            u.AddToSet("c" + parentCounterId + ".sources",
                           new BsonDocument { { "name", counterSourceInfo.Name }, { "id", counterSourceInfo.Id } });
            items.Update(q, u, UpdateFlags.Upsert,SafeMode.True);
        }

        public void New_SaveCounterInstance(int parentCategoryId, int parentCounterId, CounterInstanceInfo counterInstanceInfo)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            IMongoQuery q = Query.And(Query.EQ("id", parentCategoryId));
            UpdateBuilder u = new UpdateBuilder();
              if (counterInstanceInfo != null)
                  u.AddToSet("c" + parentCounterId + ".instances",
                           new BsonDocument {{"name", counterInstanceInfo.Name}, {"id", counterInstanceInfo.Id}});
              items.Update(q, u, UpdateFlags.Upsert, SafeMode.True);
        }


        public void New_SaveCounterExtData(int parentCategoryId, int parentCounterId, CounterExtDataInfo counterExtDataInfo)
        {
            MongoCollection<BsonDocument> items = _database.GetCollection("countersInfo");
            IMongoQuery q = Query.And(Query.EQ("id", parentCategoryId));
            UpdateBuilder u = new UpdateBuilder();
            if (counterExtDataInfo != null)
                u.AddToSet("c" + parentCounterId + ".extDatas",
                           new BsonDocument { { "name", counterExtDataInfo.Name }, { "id", counterExtDataInfo.Id } });
            items.Update(q, u, UpdateFlags.Upsert, SafeMode.True);
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
   
}