using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {

    public class Merchant {

        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("fiscalCode")]
        public string FiscalCode { get; set; }

        [BsonElement("primaryActivity")]
        [BsonRepresentation(BsonType.String)]
        public MerchantActivityType PrimaryActivityType { get; set; }

        [BsonElement("addressDetails")]
        public AddressBlock Address { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("website")]
        public string WebsiteUrl { get; set; }

        [BsonElement("createdOn")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [BsonIgnoreIfNull]
        public DateTime? CreatedOn { get; set; }

        [BsonElement("lastUpdate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [BsonIgnoreIfNull]
        public DateTime? LastUpdate { get; set; }

        [BsonElement("access")]
        public List<AccessControlEntry<MerchantRole>> Access { get; set; } = new();

        [BsonElement("isDummy")]
        [BsonDefaultValue(false)]
        [BsonIgnoreIfDefault]
        public bool IsDummy { get; set; } = false;

        [BsonElement("enabled")]
        [BsonDefaultValue(false)]
        public bool Enabled { get; set; } = false;

        [BsonElement("activationCode")]
        [BsonIgnoreIfNull]
        public string ActivationCode { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
