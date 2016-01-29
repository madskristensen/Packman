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

            var metadata = VSPackage.Manager.Provider.GetPackageMetaDataAsync(package.UnquotedNameText).Result;

            if (metadata != null)
            {
                var parent = member.Parent as JSONObject;
                var children = parent?.BlockItemChildren.OfType<JSONMember>();
                var version = children?.SingleOrDefault(c => c.UnquotedNameText == "version");

                IEnumerable<string> files;

                if (version == null)
                {
                    files = metadata.Assets.SelectMany(a => a.Files);
                }
                else
                {
                    var asset = metadata.Assets.SingleOrDefault(a => a.Version.Equals(version.UnquotedValueText, StringComparison.OrdinalIgnoreCase));
                    files = asset?.Files;
                }

                if (files == null)
                    return JSONItemValidationResult.Continue;

                if (!files.Contains(item.Text.Trim('"')))
                {
                    string message = $"({VSPackage.Name}) {item.Text} is not a valid file name for {package.Name.Text}.";
                    AddError(context, item, message);
                }
            }

            return JSONItemValidationResult.Continue;
        }
    }
}
