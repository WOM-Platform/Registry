using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {

    public class Merchant {

        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("fiscalCode")]
        public string FiscalCode { get; set; }

        [BsonElement("primaryActivity")]
        [BsonRepresentation(BsonType.String)]
        public MerchantActivityType PrimaryActivityType { get; set; }

        [BsonElement("address")]
        public string Address { get; set; }

        [BsonElement("zipCode")]
        public string ZipCode { get; set; }

        [BsonElement("city")]
        public string City { get; set; }

        [BsonElement("nation")]
        public string Nation { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("website")]
        public string WebsiteUrl { get; set; }

        [BsonElement("isApproved")]
        [BsonDefaultValue(false)]
        [BsonIgnoreIfDefault]
        public bool Approved { get; set; } = false;

        [BsonElement("adminUserIds")]
        public string[] AdministratorUserIds { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
