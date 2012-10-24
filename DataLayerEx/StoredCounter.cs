using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace DataLayerEx
{
    [BsonIgnoreExtraElements]
    public class StoredCounter
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("date")]
        public DateTime Date { get; set; }

        [BsonElement("data")]
        public StoredCounterData Data { get; set; }

        [BsonElement("props")]
        [BsonDictionaryOptions(DictionaryRepresentation.Document)]
        public Dictionary<string, string> Props { get; set; }

        
    }
    [BsonIgnoreExtraElements]
    public class StoredCounterData
    {
        [BsonElement("Count")]
        public double? Count { get; set; }
        [BsonElement("Sum")]
        public double? Sum { get; set; }
        [BsonElement("Min")]
        public List<double> Min { get; set; }
        [BsonElement("Max")]
        public List<double> Max { get; set; }
        [BsonElement("Avg")]
        public List<double> Avg { get; set; }
        [BsonElement("Pcl")]
        public Dictionary<string, double> Percentiles { get; set; }
        [BsonElement("Dg")]
        public Dictionary<string, double> DistributionGroups { get; set; }
        [BsonElement("Raw")]
        public List<double> RawValues { get; set; }
    }
    

}