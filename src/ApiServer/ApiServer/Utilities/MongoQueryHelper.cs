using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace WomPlatform.Web.Api.Utilities {
    public class MongoQueryHelper {
        // Create the match condition if the user have specified the data range or not
        public static BsonDocument DateMatchCondition(DateTime? startDateTime, DateTime? endDateTime, string key, string unwindKey = null) {
            List<BsonDocument> matchConditions = new List<BsonDocument>();

            if(startDateTime.HasValue && endDateTime.HasValue) {
                matchConditions.Add(new BsonDocument(key, new BsonDocument("$gte", startDateTime.Value)));
                matchConditions.Add(new BsonDocument(key, new BsonDocument("$lte", endDateTime.Value)));
            }
            else {
                matchConditions.Add(new BsonDocument(key, new BsonDocument("$exists", true)));
            }

            return new BsonDocument("$match", new BsonDocument("$and", new BsonArray(matchConditions)));
        }

        // Creates a list of MongoDB aggregation stages that filter documents based on a specified source name.
        // If a source is provided, the function filter the instrument
        public static List<BsonDocument> SourceMatchFromVouchersCondition(ObjectId[] sourceIds) {
            List<BsonDocument> matchConditions = new List<BsonDocument>();
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
            List<BsonDocument> matchConditions = new List<BsonDocument>();
            if(merchantIds != null && merchantIds.Length > 0) {

                matchConditions.Add(
                    new BsonDocument("$match",
                        new BsonDocument("merchantId",
                            new BsonDocument("$in", new BsonArray(merchantIds))
                        )
                    )
                );
            }

            return matchConditions;
        }


        public static List<string> GenerateDateRangeWithMissingData<T>(
            DateTime? startDate,
            DateTime? endDate,
            string key,
            string netFormatDate,
            Func<DateTime, DateTime> incrementDate,
            IMongoCollection<T> collection,
            List<BsonDocument> basePipeline
        ) {
            List<string> allDates = new List<string>();

            if(startDate.HasValue && endDate.HasValue) {
                // Generate all dates between startDate and endDate
                for(DateTime date = startDate.Value.Date; date <= endDate.Value.Date; date = incrementDate(date)) {
                    allDates.Add(date.ToString(netFormatDate));
                }
            }
            else {
                // Find the oldest date in the dataset
                List<BsonDocument> oldestDatePipeline = new List<BsonDocument>(basePipeline);
                oldestDatePipeline.Add(new BsonDocument("$match",
                    new BsonDocument(key, new BsonDocument("$ne", BsonNull.Value))
                ));
                oldestDatePipeline.Add(new BsonDocument("$group",
                    new BsonDocument {
                        { "_id", BsonNull.Value },
                        { "oldestDate", new BsonDocument("$min", $"${key}") }
                    }
                ));

                BsonDocument oldestDateResult = collection.Aggregate<BsonDocument>(oldestDatePipeline).FirstOrDefault();
                DateTime oldestDate = oldestDateResult["oldestDate"].ToLocalTime();
                DateTime today = DateTime.Today;

                for(DateTime date = oldestDate.Date; date <= today.Date; date = incrementDate(date)) {
                    allDates.Add(date.ToString(netFormatDate));
                }

                if(!allDates.Contains(today.ToString(netFormatDate))) {
                    allDates.Add(today.ToString(netFormatDate));
                }
            }

            return allDates;
        }

        public static BsonDocument AimMatchCondition(string[] aimListFilter) {
            return
                new BsonDocument("$match",
                    new BsonDocument("aimCode",
                        new BsonDocument("$in",
                            new BsonArray(aimListFilter.Select(x => x.Trim()))))
                );
        }
    }


}
