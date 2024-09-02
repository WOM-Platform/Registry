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
        if (startDateTime.HasValue && endDateTime.HasValue) {
            // Ensure the dates are valid
            DateRangeHelper.CheckDateValidity(startDateTime.Value, endDateTime.Value);

            // Apply the date range filter
            matchConditions.Add(
                new BsonDocument(key, new BsonDocument {
                    { "$gte", startDateTime.Value },
                    { "$lte", endDateTime.Value }
                })
            );
        } else {
            // Ensure that the field exists if no dates are provided
            matchConditions.Add(new BsonDocument(key, new BsonDocument("$exists", true)));
        }

        return matchConditions;
    }

    // Creates a list of MongoDB aggregation stages that filter documents based on a specified source name.
    // If a source is provided, the function filter the instrument
    public static List<BsonDocument> SourceMatchFromVouchersCondition(string? source) {
        var matchConditions = new List<BsonDocument>();
        if(source != null) {
            matchConditions.Add(
                new BsonDocument("$lookup",
                    new BsonDocument {
                        { "from", "Sources" },
                        { "localField", "generationRequest.sourceId" },
                        { "foreignField", "_id" },
                        { "as", "source" }
                    })
            );
            matchConditions.Add(
                new BsonDocument("$unwind",
                    new BsonDocument {
                        { "path", "$source" },
                        { "includeArrayIndex", "string" },
                        { "preserveNullAndEmptyArrays", true }
                    })
            );
            matchConditions.Add(
                new BsonDocument("$match",
                    new BsonDocument("source.name",
                        new BsonDocument("$eq", source))
                ));
        }

        return matchConditions;
    }

    // Creates a list of MongoDB aggregation stages that filter documents based on a specified merchant name.
    // If a source is provided, the function filter the instrument
    public static List<BsonDocument> MerchantMatchFromPaymentRequestsCondition(string? merchantName) {
        var matchConditions = new List<BsonDocument>();

        if(merchantName.HasValue()) {
            matchConditions.Add(new BsonDocument("$lookup",
                new BsonDocument {
                    { "from", "Pos" },
                    { "localField", "posId" },
                    { "foreignField", "_id" },
                    { "as", "pos" }, {
                        "pipeline",
                        new BsonArray {
                            new BsonDocument("$lookup",
                                new BsonDocument {
                                    { "from", "Merchants" },
                                    { "localField", "merchantId" },
                                    { "foreignField", "_id" },
                                    { "as", "merchant" }
                                }),
                            new BsonDocument("$unwind", "$merchant"),
                            new BsonDocument("$match",
                                new BsonDocument("merchant.name", merchantName))
                        }
                    }
                }));

            matchConditions.Add(new BsonDocument("$unwind",
                new BsonDocument {
                    { "path", "$pos" },
                    { "includeArrayIndex", "string" },
                    { "preserveNullAndEmptyArrays", false }
                }));
        }
        return matchConditions;
    }
}
