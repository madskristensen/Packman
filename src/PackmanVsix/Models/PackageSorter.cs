using System;
using System.Collections.Generic;
using Packman;
using PackmanVsix.Controls.Search;

namespace PackmanVsix.Models
{
    public class PackageSorter : IComparer<ISearchItem>
    {
        private readonly PackageSearchUtil _searchUtil;

        private PackageSorter(string searchTerm)
        {
            _searchUtil = PackageSearchUtil.ForTerm(searchTerm);
        }

        public static IComparer<ISearchItem> For(string searchTerm, IPackageProvider provider)
        {
            return new PackageSorter(searchTerm);
        }

        public int Compare(ISearchItem x, ISearchItem y)
        {
            int leftMatchStrength = _searchUtil.CalculateMatchStrength(x);
            int rightMatchStrength = _searchUtil.CalculateMatchStrength(y);
            int result = -leftMatchStrength.CompareTo(rightMatchStrength);

            if (result == 0)
            {
                result = StringComparer.OrdinalIgnoreCase.Compare(x.CollapsedItemText, y.CollapsedItemText);
            }

            return result;
        }
    }
}