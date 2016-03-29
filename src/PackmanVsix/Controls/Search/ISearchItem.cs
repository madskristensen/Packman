namespace PackmanVsix.Controls.Search
{
    public interface ISearchItem
    {
        string CollapsedItemText { get; }

        bool IsMatchForSearchTerm(string searchTerm);
    }
}
