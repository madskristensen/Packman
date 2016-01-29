using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Packman;

namespace PackmanVsix
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name("VersionCompletionProvider")]
    internal class VersionCompletionProvider : IJSONCompletionListProvider
    {
        private static StandardGlyphGroup _glyph = StandardGlyphGroup.GlyphArrow;

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.PropertyValue; }
        }

        public IEnumerable<JSONCompletionEntry> GetListEntries(JSONCompletionContext context)
        {
            ITextDocument document;
            if (TextDocumentFactoryService.TryGetTextDocument(context.Snapshot.TextBuffer, out document))
            {
                string fileName = Path.GetFileName(document.FilePath).ToLowerInvariant();

                if (string.IsNullOrEmpty(fileName) || !fileName.Equals(Defaults.ManifestFileName, StringComparison.OrdinalIgnoreCase))
                    yield break;
            }
            else
            {
                yield break;
            }

            if (!VSPackage.Manager.Provider.IsInitialized)
                yield break;

            JSONMember member = context.ContextItem as JSONMember;

            if (member == null || member.UnquotedNameText != "version")
                yield break;

            string name = (member.Parent?.Parent as JSONMember)?.UnquotedNameText;

            if (!string.IsNullOrEmpty(name))
            {
                var versions = VSPackage.Manager.Provider.GetVersionsAsync(name).Result;
                if (versions != null)
                {
                    foreach (var version in versions)
                    {
                        yield return new SimpleCompletionEntry(version, _glyph, context.Session);
                    }
                }
            }
        }
    }
}