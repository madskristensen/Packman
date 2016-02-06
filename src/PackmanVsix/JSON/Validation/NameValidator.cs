using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Core.Validation;
using Microsoft.VisualStudio.Utilities;

namespace PackmanVsix.JSON.Validation
{
    [Export(typeof(IJSONItemValidator))]
    [Name(nameof(NameValidator))]
    class NameValidator : BaseValidator
    {
        protected override Type ItemType
        {
            get { return typeof(JSONMember); }
        }

        protected override JSONItemValidationResult ValidateJsonItem(JSONParseItem item, IJSONValidationContext context)
        {
            var member = item as JSONMember;
            var packages = item.Parent?.Parent as JSONMember;

            if (packages == null || packages.UnquotedNameText != "packages")
                return JSONItemValidationResult.Continue;

            var children = (member.Value as JSONObject)?.BlockItemChildren?.OfType<JSONMember>();

            if (!children.Any(c => c.UnquotedNameText == "version"))
                return JSONItemValidationResult.Continue;

            var names = VSPackage.Manager.Provider.GetPackageNamesAsync().Result;

            if (names != null && !names.Contains(member.UnquotedNameText))
            {
                string message = $"({VSPackage.Name}) The package \"{member.UnquotedNameText}\" does not exist";
                AddError(context, member.Name, message);
            }

            return JSONItemValidationResult.Continue;
        }
    }
}
