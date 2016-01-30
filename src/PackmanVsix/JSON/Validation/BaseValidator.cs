using System;
using System.Collections.Generic;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Core.Validation;

namespace PackmanVsix.JSON.Validation
{
    abstract class BaseValidator : IJSONItemValidator
    {
        protected abstract Type ItemType { get; }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { ItemType }; }
        }

        public JSONItemValidationResult ValidateItem(JSONParseItem item, IJSONValidationContext context)
        {
            if (!VSPackage.Manager.Provider.IsInitialized)
                return JSONItemValidationResult.Continue;

            return ValidateJsonItem(item, context);
        }

        protected abstract JSONItemValidationResult ValidateJsonItem(JSONParseItem item, IJSONValidationContext context);

        protected static void AddError(IJSONValidationContext context, JSONParseItem item, string message)
        {
            if (item == null)
                return;

            var error = new JsonErrorTag
            {
                Flags = JSONErrorFlags.ErrorListError | JSONErrorFlags.UnderlineRed,
                Item = item,
                Start = item.Start,
                AfterEnd = item.AfterEnd,
                Length = item.Length,
                Text = message
            };

            context.AddError(error);
        }
    }
}
