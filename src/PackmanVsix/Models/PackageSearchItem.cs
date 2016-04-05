using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.Imaging;
using Packman.Providers;
using PackmanVsix.Controls.Search;
using PackmanVsix.Properties;

namespace PackmanVsix.Models
{
    internal class PackageSearchItem : BindableBase, ISearchItem, IPackageInfo
    {
        private readonly Dispatcher _dispatcher;
        private string _description;
        private string _homepage;
        private ImageSource _icon;
        private Lazy<Task<IPackageInfo>> _infoTask;
        private static ConcurrentDictionary<string, PackageSearchItem> _cache = new ConcurrentDictionary<string, PackageSearchItem>();
        private bool _special;
        private static PackageSearchItem _missing;

        public static PackageSearchItem Missing
        {
            get { return _missing ?? (_missing = new PackageSearchItem()); }
        }

        public static PackageSearchItem GetOrCreate(string name, string alias = null)
        {
            return _cache.GetOrAdd(name, n => new PackageSearchItem(n, alias));
        }

        private PackageSearchItem()
        {
            _special = true;
            CollapsedItemText = Resources.PackagesCouldNotBeLoaded;
        }

        private PackageSearchItem(string name, string alias = null)
        {
            Alias = alias ?? name;
            _dispatcher = Dispatcher.CurrentDispatcher;
            CollapsedItemText = name;
            Icon = WpfUtil.GetIconForImageMoniker(KnownMonikers.Package, 24, 24);
            _infoTask = new Lazy<Task<IPackageInfo>>(() => VSPackage.Manager.Provider.GetPackageInfoAsync(CollapsedItemText));
        }

        public string CollapsedItemText { get; }

        public bool IsMatchForSearchTerm(string searchTerm)
        {
            return PackageSearchUtil.ForTerm(searchTerm).IsMatch(this);
        }
        
        public string Name => CollapsedItemText;

        public string Description
        {
            get
            {
                if (!_special && !_infoTask.Value.IsCompleted)
                {
                    LoadPackageInfoAsync();

                    if (!_infoTask.Value.IsCompleted)
                    {
                        return Resources.Loading;
                    }
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

        public string Alias { get; }

        private async void LoadPackageInfoAsync()
        {
            IPackageInfo info = await _infoTask.Value.ConfigureAwait(false);

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
