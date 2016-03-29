using System;
using System.Collections.Generic;
using Packman;
using PackmanVsix.Controls.Search;

namespace PackmanVsix.Models
{
    public class PackageSorter : IComparer<ISearchItem>
    {
        private readonly string _searchTerm;

        private PackageSorter(string searchTerm)
        {
            _searchTerm = searchTerm;
        }

        public static IComparer<ISearchItem> For(string searchTerm, IPackageProvider provider)
        {
            //TODO: Do something with the package provider to get special mappings
            return new PackageSorter(searchTerm);
        }

        public int Compare(ISearchItem x, ISearchItem y)
        {
            int indexX = x.CollapsedItemText.IndexOf(_searchTerm, StringComparison.OrdinalIgnoreCase);
            int indexY = y.CollapsedItemText.IndexOf(_searchTerm, StringComparison.OrdinalIgnoreCase);
            int result = indexX.CompareTo(indexY);

            if (result == 0)
            {
                result = StringComparer.OrdinalIgnoreCase.Compare(x.CollapsedItemText, y.CollapsedItemText);
            }

            return result;
        }
    }
}