using System;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Packman;

namespace PackmanVsix
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("json")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class JsonCreationListener : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public async void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            if (VSPackage.Manager.Provider.IsInitialized)
                return;

            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            ITextDocument document;
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
            {
                string fileName = Path.GetFileName(document.FilePath);

                if (fileName.Equals(Defaults.ManifestFileName, StringComparison.OrdinalIgnoreCase))
                {
                    await VSPackage.Manager.Provider.InitializeAsync();
                }
            }
        }
    }
}
