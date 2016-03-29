using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace PackmanVsix.Controls.Search
{
    public static class SearchHighlight
    {
        public static readonly DependencyProperty HighlightStyleProperty = DependencyProperty.RegisterAttached(
            "HighlightStyle", typeof (Style), typeof (SearchHighlight), new PropertyMetadata(default(Style)));

        public static void SetHighlightStyle(DependencyObject element, Style value)
        {
            element.SetValue(HighlightStyleProperty, value);
        }

        public static Style GetHighlightStyle(DependencyObject element)
        {
            return (Style) element.GetValue(HighlightStyleProperty);
        }

        public static readonly DependencyProperty HighlightTextProperty = DependencyProperty.RegisterAttached(
            "HighlightText", typeof (string), typeof (SearchHighlight), new PropertyMetadata(default(string), TextChanged));

        private static void TextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Label lbl = d as Label;

            if (lbl == null)
            {
                return;
            }

            TextBlock block = lbl.Content as TextBlock;

            if (block == null)
            {
                string lblChild = lbl.Content as string;

                if (lblChild == null)
                {
                    return;
                }

                TextBlock newChild = new TextBlock {Text = lblChild};
                lbl.Content = newChild;
                block = newChild;
            }

            string searchText = e.NewValue as string;

            if (searchText != null)
            {
                string blockText = block.Text;
                block.Inlines.Clear();
                string pattern = Regex.Escape(searchText);
                Regex r = new Regex(pattern, RegexOptions.IgnoreCase);
                MatchCollection matches = r.Matches(blockText);
                int last = 0;

                for (int i = 0; i < matches.Count; ++i)
                {
                    if (last < matches[i].Index)
                    {
                        string inserted = blockText.Substring(last, matches[i].Index - last);
                        block.Inlines.Add(inserted);
                        last += inserted.Length;
                    }

                    Run highlight = new Run(matches[i].Value);
                    highlight.SetBinding(FrameworkContentElement.StyleProperty, new Binding
                    {
                        Mode = BindingMode.OneWay,
                        Source = d,
                        Path = new PropertyPath(HighlightStyleProperty)
                    });
                    block.Inlines.Add(highlight);
                    last += matches[i].Length;
                }

                if (last < blockText.Length)
                {
                    block.Inlines.Add(blockText.Substring(last));
                }
            }
        }

        public static void SetHighlightText(DependencyObject element, string value)
        {
            element.SetValue(HighlightTextProperty, value);
        }

        public static string GetHighlightText(DependencyObject element)
        {
            return (string) element.GetValue(HighlightTextProperty);
        }
    }
}
