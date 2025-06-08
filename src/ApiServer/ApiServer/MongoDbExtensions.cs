using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace WomPlatform.Web.Api {
    public static class MongoDbExtensions {
        /// <summary>
        /// Verifies that the update has been acknowledged nd the number of matches and updates is correct.
        /// </summary>
        public static void Verify(this UpdateResult results, int expectedMatchesAndUpdates) {
            if(!results.IsAcknowledged) {
                throw new InvalidOperationException("Database operation not acknowledged");
            }
            if(results.MatchedCount != expectedMatchesAndUpdates || results.ModifiedCount != expectedMatchesAndUpdates) {
                throw new InvalidOperationException($"Failed to verify database operation (matched {results.MatchedCount}, modified {results.ModifiedCount})");
            }
        }

        /// <summary>
        /// Performs a filtered and paged list query.
        /// </summary>
        /// <param name="collection">Source collection to filter.</param>
        /// <param name="filters">List of filters to apply using an AND clause.</param>
        /// <param name="sort">Sort definition.</param>
        /// <param name="page">Page to query (starting from 1).</param>
        /// <param name="pageSize">Elements to return by page.</param>
        public static async Task<(List<T> Results, long Total)> FilteredPagedListAsync<T>(
            this IMongoCollection<T> collection,
            IList<FilterDefinition<T>> filters,
            SortDefinition<T> sort,
            int page, int pageSize) {

            var effectiveFilter = (filters != null && filters.Count > 0) ? Builders<T>.Filter.And(filters) : Builders<T>.Filter.Empty;

            var taskCount = collection.CountDocumentsAsync(effectiveFilter);

            var list = collection.Find(effectiveFilter);
            if(sort != null) {
                list = list.Sort(sort);
            }
            list = list.Skip((page, pageSize).GetSkip()).Limit(pageSize);
            var taskList = list.ToListAsync();

            await Task.WhenAll(taskCount, taskList);

            return (taskList.Result, taskCount.Result);
        }
    }
}
