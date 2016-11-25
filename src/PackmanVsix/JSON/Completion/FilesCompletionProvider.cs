using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Utilities;

namespace PackmanVsix
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name(nameof(FilesCompletionProvider))]
    class FilesCompletionProvider : BaseCompletionProvider
    {

        public override JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.ArrayElement; }
        }

        protected override IEnumerable<JSONCompletionEntry> GetEntries(JSONCompletionContext context)
        {
            var member = context.ContextItem.FindType<JSONMember>();
            var list = new List<JSONCompletionEntry>();

            if (member == null || member.UnquotedNameText != "files")
                return list;

            var parent = member.Parent as JSONObject;
            var name = parent?.FindType<JSONMember>()?.UnquotedNameText;

            if (string.IsNullOrEmpty(name))
                return list;

            var children = parent.BlockItemChildren?.OfType<JSONMember>();
            var version = children?.SingleOrDefault(c => c.UnquotedNameText == "version");

            if (version == null)
                return list;

            var package = VSPackage.Manager.Provider.GetInstallablePackageAsync(name, version.UnquotedValueText).Result;

            if (package == null)
                return list;

            JSONArray array = context.ContextItem.FindType<JSONArray>();

            if (array == null)
                return list;

            HashSet<string> usedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (JSONArrayElement arrayElement in array.Elements)
            {
                JSONTokenItem token = arrayElement.Value as JSONTokenItem;

                if (token != null)
                {
                    usedFiles.Add(token.CanonicalizedText);
                }
            }

            FrameworkElement o = context.Session.Presenter as FrameworkElement;

            if (o != null)
            {
                o.SetBinding(ImageThemingUtilities.ImageBackgroundColorProperty, new Binding("Background")
                {
                    Source = o,
                    Converter = new BrushToColorConverter()
                });
            }

            foreach (var file in package.AllFiles)
            {
                if (!usedFiles.Contains(file))
                {
                    bool isThemeIcon;
                    ImageSource glyph = WpfUtil.GetIconForFile(o, file, out isThemeIcon);

                    list.Add(new SimpleCompletionEntry(file, glyph, context.Session));
                }
            }

            if (o != null)
            {
                BindingOperations.ClearBinding(o, ImageThemingUtilities.ImageBackgroundColorProperty);
            }

            return list;
        }
    }
}