using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace PackmanVsix
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name(nameof(FilesCompletionProvider))]
    class FilesCompletionProvider : BaseCompletionProvider
    {
        static readonly StandardGlyphGroup _glyph = StandardGlyphGroup.GlyphGroupVariable;

        public override JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.ArrayElement; }
        }

        protected override IEnumerable<JSONCompletionEntry> GetEntries(JSONCompletionContext context)
        {
            var member = context.ContextItem.FindType<JSONMember>();

            if (member == null || member.UnquotedNameText != "files")
                yield break;

            var parent = member.Parent as JSONObject;
            var name = parent.FindType<JSONMember>()?.UnquotedNameText;

            if (string.IsNullOrEmpty(name))
                yield break;

            var metadata = VSPackage.Manager.Provider.GetPackageMetaDataAsync(name).Result;

            if (metadata != null)
            {
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
                    yield break;

                foreach (var file in files)
                {
                    yield return new SimpleCompletionEntry(file, _glyph, context.Session);
                }
            }
        }
    }
}