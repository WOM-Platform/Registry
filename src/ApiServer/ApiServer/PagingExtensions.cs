using System;

namespace WomPlatform.Web.Api {
    public static class PagingExtensions {

        /// <summary>
        /// Get amount of documents to skip, given paging parameters.
        /// </summary>
        public static int GetSkip(this (int Page, int PageSize) parms) {
            if(parms.Page < 1) {
                throw new ArgumentException("Page must be greater than or equal to 1");
            }
            if(parms.PageSize < 1) {
                throw new ArgumentException("Page size must be greater than or equal to 1");
            }

            return (parms.Page - 1) * parms.PageSize;
        }

    }
}
