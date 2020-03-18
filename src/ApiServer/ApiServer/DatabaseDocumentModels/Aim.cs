using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Options;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {

    public class Aim {

        [BsonId(IdGenerator = typeof(NullIdChecker))]
        public string Code { get; set; }

        [BsonElement("titles")]
        [BsonDictionaryOptions(Representation = DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, string> Titles { get; set; }

        [BsonElement("order")]
        [BsonDefaultValue(0)]
        [BsonIgnoreIfDefault]
        public int Order { get; set; } = 0;

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
