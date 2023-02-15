using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class AccessControlEntry<R> {
        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

        [BsonElement("role")]
        [BsonRepresentation(BsonType.String)]
        public R Role { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }
    }

    public static class AccessControlEntryExtensions {
        public static void Set<R>(this IList<AccessControlEntry<R>> list, ObjectId userId, R role) {
            if(list == null) {
                throw new ArgumentNullException();
            }

            var entry = list.Where(e => e.UserId == userId).SingleOrDefault();
            if(entry != null) {
                entry.Role = role;
            }
            else {
                list.Add(new AccessControlEntry<R> {
                    UserId = userId,
                    Role = role,
                });
            }
        }

        public static void Upgrade<R>(this IList<AccessControlEntry<R>> list, ObjectId userId, R role) where R : IComparable {
            if(list == null) {
                throw new ArgumentNullException();
            }

            var entry = list.Where(e => e.UserId == userId).SingleOrDefault();
            if(entry != null) {
                if(entry.Role.CompareTo(role) < 0) {
                    entry.Role = role;
                }
            }
            else {
                list.Add(new AccessControlEntry<R> {
                    UserId = userId,
                    Role = role,
                });
            }
        }

        public static void Delete<R>(this IList<AccessControlEntry<R>> list, ObjectId userId) {
            if(list == null) {
                throw new ArgumentNullException();
            }

            var entry = list.Where(e => e.UserId == userId).SingleOrDefault();
            if(entry != null) {
                list.Remove(entry);
            }
        }

        public static AccessControlEntry<R> Get<R>(this IList<AccessControlEntry<R>> list, ObjectId userId) {
            if(list == null) {
                return null;
            }

            return list.Where(e => e.UserId == userId).SingleOrDefault();
        }

        public static bool IsAtLeast<R>(this IList<AccessControlEntry<R>> list, ObjectId userId, R role) where R : IComparable {
            if(list == null) {
                return false;
            }

            var entry = list.Where(e => e.UserId == userId).SingleOrDefault();
            if(entry == null) {
                return false;
            }

            return entry.Role.CompareTo(role) >= 0;
        }
    }
}
