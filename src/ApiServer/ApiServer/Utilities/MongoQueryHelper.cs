using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace WomPlatform.Web.Api.Utilities;

public class MongoQueryHelper {
    // Create the match condition if the user have specified the data range or not
    public static List<BsonDocument> DateMatchCondition(DateTime? startDateTime, DateTime? endDateTime, string key, string unwindKey = null) {
        var matchConditions = new List<BsonDocument>();
        // Check if filter by date
        if(startDateTime.HasValue && endDateTime.HasValue) {
            if(unwindKey != null) {
                matchConditions.Add(
                    new BsonDocument("$unwind",
                        new BsonDocument {
                            { "path", unwindKey },
                            { "includeArrayIndex", "string" },
                            { "preserveNullAndEmptyArrays", false }
                        }));
            }

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
    public static List<BsonDocument> SourceMatchFromVouchersCondition(ObjectId[] sourceIds) {
        var matchConditions = new List<BsonDocument>();
        if(sourceIds.Length > 0) {
            matchConditions.Add(
                new BsonDocument("$match",
                    new BsonDocument("generationRequest.sourceId",
                        new BsonDocument("$in", new BsonArray(sourceIds))
                    )
                )
            );
        }

        return matchConditions;
    }

    public static List<BsonDocument> MerchantMatchFromPaymentRequestsCondition(ObjectId[]? merchantIds) {
        var matchConditions = new List<BsonDocument>();

        if(merchantIds != null && merchantIds.Length > 0) {
            matchConditions.Add(
                new BsonDocument("$lookup",
                    new BsonDocument {
                        { "from", "Pos" },
                        { "localField", "posId" },
                        { "foreignField", "_id" },
                        { "as", "pos" }
                    }
                )
            );
            matchConditions.Add(
                new BsonDocument("$unwind",
                    new BsonDocument {
                        { "path", "$pos" },
                        { "includeArrayIndex", "string" },
                        { "preserveNullAndEmptyArrays", false }
                    }
                )
            );
            matchConditions.Add(
                new BsonDocument("$match",
                    new BsonDocument("pos.merchantId",
                        new BsonDocument("$in", new BsonArray(merchantIds))
                    )
                )
            );
        }

        return matchConditions;
    }


    public static List<BsonDocument> DatePaymentConfirmationCondition(DateTime? startDateTime, DateTime? endDateTime,
        string key) {
        var matchConditions = new List<BsonDocument>();
        if(startDateTime.HasValue && endDateTime.HasValue) {
            matchConditions.Add(
                new BsonDocument("$unwind",
                    new BsonDocument {
                        { "path", "$confirmations" },
                        { "includeArrayIndex", "string" },
                        { "preserveNullAndEmptyArrays", false }
                    }));
            matchConditions.Add(
                new BsonDocument("$match",
                    new BsonDocument(key,
                        new BsonDocument {
                            {
                                "$gte",
                                startDateTime
                            }, {
                                "$lte",
                                endDateTime
                            }
                        }))
            );
        }

        return matchConditions;
    }
}
