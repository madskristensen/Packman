using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Text;
using Packman;

namespace PackmanVsix
{
    abstract class BaseCompletionProvider : IJSONCompletionListProvider
    {
        static readonly IEnumerable<JSONCompletionEntry> _empty = Enumerable.Empty<JSONCompletionEntry>();

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public abstract JSONCompletionContextType ContextType { get; }

        public IEnumerable<JSONCompletionEntry> GetListEntries(JSONCompletionContext context)
        {
            ITextDocument document;

            if (TextDocumentFactoryService.TryGetTextDocument(context.Snapshot.TextBuffer, out document))
            {
                string fileName = Path.GetFileName(document.FilePath);

                if (fileName.Equals(Defaults.ManifestFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return GetEntries(context);
                }
            }

            return _empty;
        }

        protected abstract IEnumerable<JSONCompletionEntry> GetEntries(JSONCompletionContext context);
    }
}