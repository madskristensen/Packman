using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Core.Validation;
using Microsoft.VisualStudio.Utilities;

namespace PackmanVsix.JSON.Validation
{
    [Export(typeof(IJSONItemValidator))]
    [Name(nameof(FileValidator))]
    class FileValidator : BaseValidator
    {
        protected override Type ItemType
        {
            get { return typeof(JSONArrayElement); }
        }

        protected override JSONItemValidationResult ValidateJsonItem(JSONParseItem item, IJSONValidationContext context)
        {
            var member = item.FindType<JSONMember>();

            if (member == null || member.UnquotedNameText != "files")
                return JSONItemValidationResult.Continue;

            var package = member.Parent?.Parent as JSONMember;
            var packages = package?.Parent?.Parent as JSONMember;

            if (package == null || packages == null || packages.UnquotedNameText != "packages")
                return JSONItemValidationResult.Continue;

            var parent = member.Parent as JSONObject;
            var children = parent?.BlockItemChildren.OfType<JSONMember>();
            var version = children?.SingleOrDefault(c => c.UnquotedNameText == "version");

            if (version == null)
                return JSONItemValidationResult.Continue;

            var installable = VSPackage.Manager.Provider.GetInstallablePackageAsync(package.UnquotedNameText, version.UnquotedValueText).Result;

            if (installable == null)
                return JSONItemValidationResult.Continue;

            if (!installable.AllFiles.Contains(item.Text.Trim('"', ',')))
            {
                string message = $"({VSPackage.Name}) {item.Text} is not a valid file name for {installable.Name} {installable.Version}.";
                AddError(context, item, message);
            }

            return JSONItemValidationResult.Continue;
        }
    }
}
