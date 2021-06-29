using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MongoDB.Bson;

namespace WomPlatform.Web.Api.Conversion {

    /// <summary>
    /// Allows direct model binding to ObjectId values.
    /// </summary>
    class ObjectIdModelBinderProvider : IModelBinderProvider {

        private class ObjectIdModelBinder : IModelBinder {

            public Task BindModelAsync(ModelBindingContext bindingContext) {
                if(bindingContext == null) {
                    throw new ArgumentNullException(nameof(bindingContext));
                }

                var modelName = bindingContext.ModelName;

                // Check for not provided values
                var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
                if(valueProviderResult == ValueProviderResult.None || valueProviderResult.FirstValue == null) {
                    return Task.CompletedTask;
                }

                // Parse ID
                bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);
                var stringId = valueProviderResult.FirstValue;
                if(!ObjectId.TryParse(stringId, out ObjectId objId)) {
                    bindingContext.ModelState.TryAddModelError(modelName, "Invalid ID format");
                    return Task.CompletedTask;
                }

                // Set on model
                bindingContext.Result = ModelBindingResult.Success(objId);
                return Task.CompletedTask;
            }

        }

        public IModelBinder GetBinder(ModelBinderProviderContext context) {
            if(context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if(context.Metadata.ModelType == typeof(ObjectId)) {
                return new ObjectIdModelBinder();
            }

            return null;
        }

    }

}
