using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace PackmanVsix
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name(nameof(NameCompletionProvider))]
    class NameCompletionProvider : BaseCompletionProvider
    {
        static readonly StandardGlyphGroup _glyph = StandardGlyphGroup.GlyphGroupVariable;

        public override JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.PropertyName; }
        }

        protected override IEnumerable<JSONCompletionEntry> GetEntries(JSONCompletionContext context)
        {
            if (!VSPackage.Manager.Provider.IsInitialized)
                yield break;

            string parent = (context.ContextItem.Parent?.Parent as JSONMember)?.UnquotedNameText;

            if (parent != "packages")
                yield break;

            var names = VSPackage.Manager.Provider.GetPackageNamesAsync().Result;
            if (names != null)
            {
                foreach (var name in names)
                {
                    yield return new SimpleCompletionEntry(name, _glyph, context.Session);
                }
            }
        }
    }
}