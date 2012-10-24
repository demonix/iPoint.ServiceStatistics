using System;
using System.Collections.Generic;
using System.Linq;
using Aggregation;
using CountersDataLayer.CountersCache;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace CountersDataLayer
{
    public class CountersDatabase
    {
        private static object _locker = new object();
        private Cache _countersMapper = null;
        private object _countersMapperLock = new object();
        private MongoServer _server;


        private CountersDatabase(MongoServer server, MongoDatabase database)
        {
            _server = server;
            Database = database;
        }

        public MongoDatabase Database { get; private set; }

        public Cache CountersMapper
        {
            get
            {
                if (_countersMapper == null)
                    lock (_countersMapperLock)
                    {
                        _countersMapper = new Cache();
                    }
                return _countersMapper;
            }
        }

        public static CountersDatabase Instance { get; private set; }


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

        public static CountersDatabase Connect(string mongoUrl)
        {
            MongoUrlBuilder builder = new MongoUrlBuilder(mongoUrl);
            builder.SocketTimeout = new TimeSpan(0, 30, 0);
            //builder.Server = port.HasValue ? new MongoServerAddress(host, port.Value) : new MongoServerAddress(host);
            MongoServer server = MongoServer.Create(builder.ToServerSettings());
            server.Connect();
            MongoDatabase db = server.GetDatabase(builder.DatabaseName);
            return new CountersDatabase(server, db);
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

        public void SaveCounters(TotalAggregationResult counters)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersData");

            BsonDocument cData = new BsonDocument()
                                     {
                                         counters.ResultGroups.Select(
                                             r =>
                                             new BsonElement(CountersMapper.Map(counters.CounterCategory,
                                                                                counters.CounterName,
                                                                                r.CounterGroup.Source,
                                                                                r.CounterGroup.Instance,
                                                                                r.CounterGroup.ExtendedData),
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


        public IEnumerable<CounterCategoryInfo> GetCounterCategories2()
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersInfo");
            var cursor = items.FindAll();
            cursor.SetFields(Fields.Include("category", "id"));
            return cursor.Select(d => new CounterCategoryInfo(d["category"].AsString, d["id"].AsInt32));
        }

        public CounterCategoryInfo GetCounterCategory2(string categoryName)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersInfo");
            QueryComplete q = Query.EQ("category", categoryName);
            var cursor = items.FindAll();
            cursor.SetFields(Fields.Include("category", "id"));
            var res = cursor.FirstOrDefault();
            return res == null ? null : new CounterCategoryInfo(res["category"].AsString, res["id"].AsInt32);
        }

        public IEnumerable<string> GetCounterCategories(DateTime beginDate, DateTime endDate)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersData");
            QueryComplete qb = Query.GTE("date", beginDate).LTE(endDate);
            var cursor = items.Find(qb);
            cursor.SetFields(Fields.Include("counterCategory"));
            return cursor.Select(d => d["counterCategory"].AsString).Distinct();
        }


        public IEnumerable<CounterNameInfo> GetCounterNames2(int categoryId)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersInfo");
            QueryComplete q = Query.EQ("id", categoryId);
            var cursor = items.Find(q);
            cursor.SetFields(Fields.Include("counters"));
            var counter = cursor.FirstOrDefault();
            if (counter == null || !counter.Contains("counters"))
                return Enumerable.Empty<CounterNameInfo>();
            return
                counter["counters"].AsBsonArray.Select(
                    d =>
                    new CounterNameInfo(d.AsBsonDocument["name"].AsString, d.AsBsonDocument["id"].AsInt32, categoryId));
        }

        public IEnumerable<CounterSourceInfo> GetCounterSources(int categoryId, int counterId)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersInfo");
            QueryComplete q = Query.EQ("id", categoryId);
            var cursor = items.Find(q);
            cursor.SetFields(Fields.Include("c" + counterId + ".sources"));
            var counter = cursor.FirstOrDefault();
            if (counter == null || !counter.Contains("c" + counterId)|| !counter["c" + counterId].AsBsonDocument.Contains("sources"))
                return Enumerable.Empty<CounterSourceInfo>();
            return
                counter["c" + counterId].AsBsonDocument["sources"].AsBsonArray.Select(d => new CounterSourceInfo(d.AsBsonDocument["name"].AsString, d.AsBsonDocument["id"].AsInt32));
        }

        public IEnumerable<CounterInstanceInfo> GetCounterInstances(int categoryId, int counterId)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersInfo");
            QueryComplete q = Query.EQ("id", categoryId);
            var cursor = items.Find(q);
            cursor.SetFields(Fields.Include("c" + counterId + ".instances"));
            var counter = cursor.FirstOrDefault();
            if (counter == null || !counter.Contains("c" + counterId) || !counter["c" + counterId].AsBsonDocument.Contains("instances"))
                return Enumerable.Empty<CounterInstanceInfo>();
            return
                counter["c" + counterId].AsBsonDocument["instances"].AsBsonArray.Select(d => new CounterInstanceInfo(d.AsBsonDocument["name"].AsString, d.AsBsonDocument["id"].AsInt32));
        }

        public IEnumerable<CounterExtDataInfo> GetCounterExtDatas(int categoryId, int counterId)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersInfo");
            QueryComplete q = Query.EQ("id", categoryId);
            var cursor = items.Find(q);
            cursor.SetFields(Fields.Include("c" + counterId + ".extDatas"));
            var counter = cursor.FirstOrDefault();
            if (counter == null || !counter.Contains("c" + counterId)|| !counter["c" + counterId].AsBsonDocument.Contains("extDatas"))
                return Enumerable.Empty<CounterExtDataInfo>();
            return
                counter["c" + counterId].AsBsonDocument["extDatas"].AsBsonArray.Select(d => new CounterExtDataInfo(d.AsBsonDocument["name"].AsString, d.AsBsonDocument["id"].AsInt32));
        }

     

        public IEnumerable<string> GetCounterNames(DateTime beginDate, DateTime endDate, string counterCategory)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersData");
            QueryComplete qb = Query.GTE("date", beginDate).LTE(endDate);
            QueryComplete qb2 = Query.EQ("counterCategory", counterCategory);
            var cursor = items.Find(Query.And(qb, qb2));
            cursor.SetFields(Fields.Include("counterName"));
            return cursor.Select(d => d["counterName"].AsString).Distinct();
        }


        public IEnumerable<CounterDetail> GetCounterDetails(DateTime beginDate, DateTime endDate, string counterCategory,
                                                            string counterName)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersData");
            QueryComplete qb = Query.GTE("date", beginDate).LTE(endDate);
            QueryComplete qb2 = Query.EQ("counterCategory", counterCategory);
            QueryComplete qb3 = Query.EQ("counterName", counterName);
            var cursor = items.Find(Query.And(qb, qb2, qb3));
            cursor.SetFields(Fields.Include("source", "instance", "extendedData"));
            return cursor.Select(d =>
                                 new CounterDetail(d["source"].AsString, d["instance"].AsString,
                                                   d.Contains("extendedData") ? d["extendedData"].AsString : null)
                );
        }

        public DateTime? GetFreshestAfterDate(string counterCategory, string counterName, DateTime date)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersData");
            IMongoQuery sq = Query.And(Query.GT("date", date),
                                       Query.EQ("counterCategory", counterCategory),
                                       Query.EQ("counterName", counterName));
            var cursor = items.Find(sq);
            cursor.Limit = 1;
            cursor.SetFields("date");
            if (cursor.Size() == 0)
                return null;
            return cursor.First()["date"].AsDateTime;
        }
        public DateTime? GetFreshestNotAfterDate(string counterCategory, string counterName, DateTime date)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersData");
            IMongoQuery sq = Query.And(Query.LTE("date", date),
                                       Query.EQ("counterCategory", counterCategory),
                                       Query.EQ("counterName", counterName));
            var cursor = items.Find(sq);
            cursor.SetSortOrder(new SortByBuilder().Descending("date"));
            cursor.Limit = 1;
            cursor.SetFields("date");
            if (cursor.Size() == 0)
                return null;
            return cursor.First()["date"].AsDateTime;
        }


        public void RemoveCountersValuesBetweenDates(string counterCategory, string counterName, DateTime leftDate, DateTime rightDate)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersData");
            IMongoQuery dq = Query.And(Query.GT("date", leftDate).LT(rightDate),
                                                   Query.EQ("counterCategory", counterCategory),
                                                   Query.EQ("counterName", counterName));
            items.Remove(dq, SafeMode.True);
        }

        public List<CounterSeriesData> GetCounterData(DateTime beginDate, DateTime endDate, int counterCategoryId,
                                                      int counterNameId, int counterSourceId, int counterInstanceId,
                                                      int counterExtDataId, List<string> seriesFilter)
        {
            List<CounterSeriesData> resultData = new List<CounterSeriesData>();
            if (seriesFilter.Count == 0)
                return resultData;
            bool getAllSeries = seriesFilter.Contains("*");
            List<string> seriesNames = new List<string>();
            
            
            MongoCollection<BsonDocument> items = Database.GetCollection("countersData");
            string mappedCategoryName = CountersMapper.GetMappedCategoryName(counterCategoryId);
            string mappedCounterName = CountersMapper.GetMappedCounterName(counterCategoryId, counterNameId);
            string mappedCounterInstance = CountersMapper.GetMappedCounterInstanceName(counterCategoryId, counterNameId,
                                                                                       counterInstanceId);
            string mappedCounterSource = CountersMapper.GetMappedCounterSourceName(counterCategoryId, counterNameId,
                                                                                   counterSourceId);
            string mappedCounterExtData = CountersMapper.GetMappedCounterExtDataName(counterCategoryId, counterNameId,
                                                                                     counterExtDataId);


            QueryComplete qb = beginDate == endDate
                                   ? Query.EQ("date", beginDate)
                                   : Query.GT("date", beginDate).LTE(endDate);

            QueryComplete qb2 = Query.EQ("counterCategory", mappedCategoryName);
            QueryComplete qb3 = Query.EQ("counterName", mappedCounterName);
            SortByBuilder sortOrder = new SortByBuilder().Ascending("date");
            var cursor = items.Find(Query.And(qb, qb2, qb3));
            cursor.SetSortOrder(sortOrder);
            string counterDescription = counterSourceId + "/" + counterInstanceId + "/" + counterExtDataId;
            cursor.SetFields(Fields.Include("type", "date", "data." + counterDescription));

            foreach (BsonDocument cnt in cursor)
            {
                var dateTime = cnt["date"].AsDateTime;
                var countersData = cnt["data"].AsBsonDocument;
                if (!countersData.Contains(counterDescription))
                {
                    foreach (string seriesName in seriesNames)
                        if (getAllSeries || seriesFilter.Contains(seriesName))
                            resultData.Find(f => f.SeriesName == seriesName).AddSeriesPoint(new SeriesPoint(dateTime, null));
                    continue;
                }
                var seriesPoints = countersData[counterDescription].AsBsonDocument;

                foreach (BsonElement seriesPoint in seriesPoints)
                {
                    string seriesName = seriesPoint.Name;
                    if (!seriesNames.Contains(seriesName))
                        seriesNames.Add(seriesName);
                    if (!getAllSeries && !seriesFilter.Contains(seriesName)) continue;
                    var value = seriesPoint.Value.IsString
                                    ? new UniversalValue(
                                          TimeSpan.Parse(seriesPoint.Value.AsString))
                                    : new UniversalValue(
                                          seriesPoint.Value.ToDouble());

                    var a = resultData.Find(f => f.SeriesName == seriesName);
                    if (a == null)
                    {
                        a = new CounterSeriesData(seriesName, value.Type, mappedCategoryName, mappedCounterName,
                                                  mappedCounterSource, mappedCounterInstance, mappedCounterExtData);
                        resultData.Add(a);
                    }
                    a.AddSeriesPoint(new SeriesPoint(dateTime, value));
                }
            }
            return resultData;
        }


        public IEnumerable<CounterNameInfo> GetCounterNamesInCategory(int categoryId)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersInfo");
            QueryComplete q = Query.EQ("id", categoryId);
            var cursor = items.Find(q);
            cursor.SetFields(Fields.Include("counters"));
            var counter = cursor.FirstOrDefault();
            if (counter == null || !counter.Contains("counters"))
                return Enumerable.Empty<CounterNameInfo>();
            return
                counter["counters"].AsBsonArray.Select(
                    d =>
                    new CounterNameInfo(d.AsBsonDocument["name"].AsString, d.AsBsonDocument["id"].AsInt32, categoryId));
        }


        public void SaveCounterCategory(CounterCategoryInfo cat)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersInfo");
            items.Insert(new BsonDocument
                             {
                                 {"category", cat.Name},
                                 {"id", cat.Id},
                                 {"counters", new BsonArray()}
                             }, SafeMode.True);
        }

        public void SaveCounterName(int parentCategoryId, CounterNameInfo nameInfo)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersInfo");
            IMongoQuery q = Query.EQ("id", parentCategoryId);
            UpdateBuilder u = new UpdateBuilder();
            u.AddToSet("counters", new BsonDocument {{"name", nameInfo.Name}, {"id", nameInfo.Id}});
            u.Set("c" + nameInfo.Id, new BsonDocument
                                         {
                                             {"sources", new BsonArray()},
                                             {"instances", new BsonArray()},
                                             {"extDatas", new BsonArray()}
                                         });
            items.Update(q, u, UpdateFlags.Upsert, SafeMode.True);
        }

        public IEnumerable<CounterCategoryInfo> GetCounterCategories()
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersInfo");
            var cursor = items.FindAll();
            cursor.SetFields(Fields.Include("category", "id"));
            return cursor.Select(d => new CounterCategoryInfo(d["category"].AsString, d["id"].AsInt32));
        }

        public void SaveCounterSource(int parentCategoryId, int parentCounterId, CounterSourceInfo counterSourceInfo)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersInfo");
            IMongoQuery q = Query.And(Query.EQ("id", parentCategoryId));
            UpdateBuilder u = new UpdateBuilder();
            u.AddToSet("c" + parentCounterId + ".sources",
                       new BsonDocument {{"name", counterSourceInfo.Name}, {"id", counterSourceInfo.Id}});
            items.Update(q, u, UpdateFlags.Upsert, SafeMode.True);
        }

        public void SaveCounterInstance(int parentCategoryId, int parentCounterId,
                                        CounterInstanceInfo counterInstanceInfo)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersInfo");
            IMongoQuery q = Query.And(Query.EQ("id", parentCategoryId));
            UpdateBuilder u = new UpdateBuilder();
            if (counterInstanceInfo != null)
                u.AddToSet("c" + parentCounterId + ".instances",
                           new BsonDocument {{"name", counterInstanceInfo.Name}, {"id", counterInstanceInfo.Id}});
            items.Update(q, u, UpdateFlags.Upsert, SafeMode.True);
        }


        public void SaveCounterExtData(int parentCategoryId, int parentCounterId,
                                       CounterExtDataInfo counterExtDataInfo)
        {
            MongoCollection<BsonDocument> items = Database.GetCollection("countersInfo");
            IMongoQuery q = Query.And(Query.EQ("id", parentCategoryId));
            UpdateBuilder u = new UpdateBuilder();
            if (counterExtDataInfo != null)
                u.AddToSet("c" + parentCounterId + ".extDatas",
                           new BsonDocument {{"name", counterExtDataInfo.Name}, {"id", counterExtDataInfo.Id}});
            items.Update(q, u, UpdateFlags.Upsert, SafeMode.True);
        }
    }

    public class CounterDetail
    {
        public CounterDetail(string source, string instance, string extData)
        {
            Source = source;
            Instance = instance;
            ExtData = extData;
        }

        public string Source { get; set; }
        public string Instance { get; set; }
        public string ExtData { get; set; }
    }
}