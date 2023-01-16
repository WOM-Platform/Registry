using System;

namespace WomPlatform.Web.Api.OutputModels {
    public class Paged<T> {
        public T[] Data { get; init; }

        public long TotalCount { get; init; }

        public int Page { get; init; }

        public int PageSize { get; init; }

        public int PageCount { get; init; }

        public bool HasPrevious { get; init; }

        public bool HasNext { get; init; }

        public static Paged<T> FromAll(T[] values) => new() {
            Data = values,
            TotalCount = values.Length,
            Page = 1,
            PageSize = values.Length,
            PageCount = 1,
            HasPrevious = false,
            HasNext = false,
        };

        public static Paged<T> FromPage(T[] values, int page, int pageSize, long totalCount) {
            int pageCount = (int)Math.Ceiling((double)totalCount / pageSize);

            return new Paged<T> {
                Data = values,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                PageCount = pageCount,
                HasPrevious = page > 0,
                HasNext = page < pageCount,
            };
        }
    }
}
