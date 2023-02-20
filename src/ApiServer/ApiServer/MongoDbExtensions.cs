using System;
using MongoDB.Driver;

namespace WomPlatform.Web.Api {
    public static class MongoDbExtensions {
        public static void Verify(this UpdateResult results, int expectedMatchesAndUpdates) {
            if(!results.IsAcknowledged) {
                throw new InvalidOperationException("Database operation not acknowledged");
            }
            if(results.MatchedCount != expectedMatchesAndUpdates || results.ModifiedCount != expectedMatchesAndUpdates) {
                throw new InvalidOperationException($"Failed to decrement request attempts (matched {results.MatchedCount}, modified {results.ModifiedCount})");
            }
        }
    }
}
