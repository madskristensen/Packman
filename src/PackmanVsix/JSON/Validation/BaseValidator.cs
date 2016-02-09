using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Core.Schema;
using Microsoft.JSON.Core.Validation;

namespace PackmanVsix
{
    abstract class BaseValidator : IJSONItemValidator
    {
        [Import]
        IJSONSchemaEvaluationReportCache _reportCache { get; set; }

        protected abstract Type ItemType { get; }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { ItemType }; }
        }

        public JSONItemValidationResult ValidateItem(JSONParseItem item, IJSONValidationContext context)
        {
            if (!VSPackage.Manager.Provider.IsInitialized)
                return JSONItemValidationResult.Continue;

            if (item.JSONDocument.HasSchema(_reportCache))
            {
                return ValidateJsonItem(item, context);
            }

            return JSONItemValidationResult.Continue;
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
