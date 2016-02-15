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
    [Name(nameof(VersionValidator))]
    class VersionValidator : BaseValidator
    {
        protected override Type ItemType
        {
            get { return typeof(JSONMember); }
        }

        protected override JSONItemValidationResult ValidateJsonItem(JSONParseItem item, IJSONValidationContext context)
        {
            var member = item as JSONMember;

            if (member.UnquotedNameText != "version")
                return JSONItemValidationResult.Continue;

            var package = item.Parent?.Parent as JSONMember;
            var packages = package?.Parent?.Parent as JSONMember;

            if (package == null ||packages == null || packages.UnquotedNameText != "packages")
                return JSONItemValidationResult.Continue;

            var versions = VSPackage.Manager.Provider.GetVersionsAsync(package.UnquotedNameText).Result;

            if (versions != null && !versions.Contains(member.UnquotedValueText))
            {
                string message = $"({Vsix.Name}) \"{member.UnquotedValueText}\" is not a valid version for \"{package.UnquotedNameText}\".";
                AddError(context, member.Value, message);
            }

            return JSONItemValidationResult.Continue;
        }
    }
}
