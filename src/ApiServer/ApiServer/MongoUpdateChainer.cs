using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace WomPlatform.Web.Api {

    public class MongoUpdateChainer<T> {
        private List<UpdateDefinition<T>> _updates = [];
        private readonly UpdateDefinitionBuilder<T> _builder;

        public MongoUpdateChainer(UpdateDefinitionBuilder<T> builder) {
            _builder = builder;
        }

        public MongoUpdateChainer<T> Add(Func<UpdateDefinitionBuilder<T>, UpdateDefinition<T>> creator) {
            _updates.Add(creator(_builder));
            return this;
        }

        public MongoUpdateChainer<T> Set<TField>(Expression<Func<T, TField>> field, TField value) {
            _updates.Add(_builder.Set(field, value));
            return this;
        }

        public UpdateDefinition<T> End() {
            return _builder.Combine(_updates);
        }
    }

    public static class MongoUpdateChainerExtensions {
        /// <summary>
        /// Combine multiple update definitions in a fluent manner.
        /// </summary>
        public static MongoUpdateChainer<T> Chain<T>(this UpdateDefinitionBuilder<T> b) {
            return new MongoUpdateChainer<T>(b);
        }
    }
}
