using System;
using System.Collections.Generic;
using MongoDB.Bson;
using RestSharp.Extensions;

namespace WomPlatform.Web.Api.Utilities;

public class MongoQueryHelper {
    // Create the match condition if the user have specified the data range or not
    public static List<BsonDocument> DateMatchCondition(DateTime? startDateTime, DateTime? endDateTime, string key) {
        var matchConditions = new List<BsonDocument>();
        // Check if the user wants to filter by date
        if(startDateTime.HasValue && endDateTime.HasValue) {
            // Ensure the dates are valid
            DateRangeHelper.CheckDateValidity(startDateTime.Value, endDateTime.Value);

            // Apply the date range filter
            matchConditions.Add(
                new BsonDocument(key, new BsonDocument {
                    { "$gte", startDateTime.Value },
                    { "$lte", endDateTime.Value }
                })
            );
        }
        else {
            // Ensure that the field exists if no dates are provided
            matchConditions.Add(new BsonDocument(key, new BsonDocument("$exists", true)));
        }

        return matchConditions;
    }

    // Creates a list of MongoDB aggregation stages that filter documents based on a specified source name.
    // If a source is provided, the function filter the instrument
    public static List<BsonDocument> SourceMatchFromVouchersCondition(ObjectId? sourceId) {
        var matchConditions = new List<BsonDocument>();
        if(sourceId.HasValue) {
            matchConditions.Add(
                new BsonDocument("$match",
                    new BsonDocument("generationRequest.sourceId",
                        new BsonDocument("$eq",
                            new ObjectId(sourceId.ToString())
                        )
                    )
                )
            );
        }

        return matchConditions;
    }

    // Creates a list of MongoDB aggregation stages that filter documents based on a specified merchant name.
    // If a source is provided, the function filter the instrument
    public static List<BsonDocument> MerchantMatchFromPaymentRequestsCondition(ObjectId? merchantId) {
        var matchConditions = new List<BsonDocument>();
        if(merchantId.HasValue) {
            matchConditions.Add(
                new BsonDocument("$lookup",
                    new BsonDocument {
                        { "from", "Pos" },
                        { "localField", "posId" },
                        { "foreignField", "_id" },
                        { "as", "pos" }
                    }));
            matchConditions.Add(
                new BsonDocument("$unwind",
                    new BsonDocument {
                        { "path", "$pos" },
                        { "includeArrayIndex", "string" },
                        { "preserveNullAndEmptyArrays", false }
                    }));

            matchConditions.Add(
                new BsonDocument("$match",
                    new BsonDocument("pos.merchantId",
                        new BsonDocument("$eq",
                            new ObjectId(merchantId.ToString()))))
            );
        }

        return matchConditions;
    }
}
