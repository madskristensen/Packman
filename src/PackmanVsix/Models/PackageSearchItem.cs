using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Packman.Providers;
using PackmanVsix.Controls.Search;
using PackmanVsix.Properties;

namespace PackmanVsix.Models
{
    internal class PackageSearchItem : BindableBase, ISearchItem, IPackageInfo
    {
        private bool _isLoaded;
        private readonly Dispatcher _dispatcher;
        private string _description;
        private string _homepage;
        private ImageSource _icon;

        public PackageSearchItem(string name)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            CollapsedItemText = name;
            Icon = WpfUtil.GetIconForImageMoniker(KnownMonikers.Package, 24, 24);
        }

        public string CollapsedItemText { get; }

        public bool IsMatchForSearchTerm(string searchTerm)
        {
            return CollapsedItemText.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) > -1;
        }
        
        public string Name => CollapsedItemText;

        public string Description
        {
            get
            {
                if (!_isLoaded)
                {
                    LoadPackageInfoAsync();
                    return Resources.Loading;
                }

                return _description;
            }
            set { Set(ref _description, value); }
        }

        public string Homepage
        {
            get { return _homepage; }
            set { Set(ref _homepage, value); }
        }

        public ImageSource Icon
        {
            get { return _icon; }
            set { Set(ref _icon, value); }
        }

        private async void LoadPackageInfoAsync()
        {
            IPackageInfo info = await VSPackage.Manager.Provider.GetPackageInfoAsync(CollapsedItemText).ConfigureAwait(false);
            _isLoaded = true;

            await _dispatcher.InvokeAsync(() =>
            {
                Description = info.Description;
                Homepage = info.Homepage;

                if (info.Icon != null)
                {
                    Icon = info.Icon;
                }
            });
        }
    }
}
