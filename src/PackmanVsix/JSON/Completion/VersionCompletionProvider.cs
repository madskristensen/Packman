using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Utilities;

namespace PackmanVsix
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name(nameof(VersionCompletionProvider))]
    class VersionCompletionProvider : BaseCompletionProvider
    {
        public override JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.PropertyValue; }
        }

        protected override IEnumerable<JSONCompletionEntry> GetEntries(JSONCompletionContext context)
        {
            var member = context.ContextItem as JSONMember;

            if (member == null || member.UnquotedNameText != "version")
                yield break;

            string name = (member.Parent?.Parent as JSONMember)?.UnquotedNameText;

            if (string.IsNullOrEmpty(name))
                yield break;

            var versions = VSPackage.Manager.Provider.GetVersionsAsync(name).Result;
            if (versions != null)
            {
                int index = 0;
                
                foreach (var version in versions)
                {
                    yield return new SimpleCompletionEntry(version, KnownMonikers.Version, context.Session, ++index);
                }
            }
        }
    }
}