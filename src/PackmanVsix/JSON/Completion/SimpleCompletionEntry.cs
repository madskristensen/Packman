using System.Windows.Media;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.Web.Editor.Imaging;

namespace PackmanVsix
{
    class SimpleCompletionEntry : JSONCompletionEntry
    {
        private static readonly ImageSource _glyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic);

        public SimpleCompletionEntry(string text, IIntellisenseSession session)
            : this(text, (ImageSource)null, session)
        { }

        public SimpleCompletionEntry(string text, StandardGlyphGroup glyph, IIntellisenseSession session)
            : this(text, null, glyph, session)
        { }

        public SimpleCompletionEntry(string text, ImageSource glyph, IIntellisenseSession session)
            : this(text, null, glyph, session)
        { }

        public SimpleCompletionEntry(string text, string description, IIntellisenseSession session)
            : base(text, "\"" + text + "\"", description, _glyph, null, false, session as ICompletionSession)
        { }

        public SimpleCompletionEntry(string text, string description, StandardGlyphGroup glyph, IIntellisenseSession session)
            : base(text, "\"" + text + "\"", description, GlyphService.GetGlyph(glyph, StandardGlyphItem.GlyphItemPublic), null, false, session as ICompletionSession)
        { }

        public SimpleCompletionEntry(string text, string description, ImageSource glyph, IIntellisenseSession session)
            : base(text, "\"" + text + "\"", description, glyph, null, false, session as ICompletionSession)
        { }

        public SimpleCompletionEntry(string displayText, string insertionText, string description, IIntellisenseSession session)
            : base(displayText, insertionText, description, _glyph, null, false, session as ICompletionSession)
        { }
    }
}