using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using Packman;

namespace PackmanVsix.Models
{
    public class InstallDialogViewModel : BindableBase
    {
        private readonly Action<bool> _closeDialog;
        private IReadOnlyList<PackageItem> _displayRoot;
        private bool _includePackageName;
        private InstallablePackage _package;
        private string _packageName;
        private IReadOnlyList<string> _packageVersions;
        private string _rootFolderName;
        private string _selectedPackageVersion;
        private IReadOnlyList<string> _availablePacakges;
        private bool _isPackageListLoaded;

        public InstallDialogViewModel(Dispatcher dispatcher, Action<bool> closeDialog)
        {
            Dispatcher = dispatcher;
            _closeDialog = closeDialog;
            IncludePackageName = VSPackage.Options.CreatePackageFolder;
            SelectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            InstallPackageCommand = ActionCommand.Create(InstallPackage, PackageIsSet, false);
            LoadPackages();
        }

        private async void LoadPackages()
        {
            IEnumerable<string> packages = await VSPackage.Manager.Provider.GetPackageNamesAsync();
            IReadOnlyList<string> listedPackages = packages?.ToList();
            bool loadSuccess = true;
            
            if (listedPackages == null || listedPackages.Count == 0)
            {
                listedPackages = new[] {Properties.Resources.PackagesCouldNotBeLoaded};
                loadSuccess = false;
            }

            Dispatcher.Invoke(() =>
            {
                AvailablePackages = listedPackages;
                IsPackageListLoaded = loadSuccess;
            });
        }

        public IReadOnlyList<string> AvailablePackages
        {
            get { return _availablePacakges; }
            set { Set(ref _availablePacakges, value); }
        }

        public Dispatcher Dispatcher { get; private set; }

        public IReadOnlyList<PackageItem> DisplayRoots
        {
            get { return _displayRoot; }
            set { Set(ref _displayRoot, value); }
        }

        public bool IncludePackageName
        {
            get { return _includePackageName; }
            set
            {
                if (Set(ref _includePackageName, value))
                {
                    IReadOnlyList<PackageItem> roots = DisplayRoots;
                    InstallablePackage pkg = Package;
                    if (roots != null && pkg != null)
                    {
                        if (value)
                        {
                            IReadOnlyList<PackageItem> children = roots[0].Children;
                            PackageItem packageItem = new PackageItem(this)
                            {
                                Name = pkg.Name,
                                ItemType = PackageItemType.Folder,
                                IsExpanded = true,
                                Children = children
                            };

                            roots[0].Children = new[] { packageItem };
                        }
                        else
                        {
                            roots[0].Children = roots[0].Children[0].Children;
                        }
                    }
                }
            }
        }

        public ICommand InstallPackageCommand { get; }

        public InstallablePackage Package
        {
            get { return _package; }
            set
            {
                if (Set(ref _package, value))
                {
                    SelectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    InstallPackageCommand.CanExecute(null);
                    
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        int tries = 0;
                        bool ran = false;

                        while (!ran && tries++ < 10)
                        {
                            try
                            {
                                RebuildPackageTree(value);
                                ran = true;
                            }
                            catch
                            {
                            }
                        }

                        if (!ran && Package == value)
                        {
                            DisplayRoots = null;
                        }
                    });
                }
            }
        }

        public string PackageName
        {
            get { return _packageName; }
            set
            {
                if (Set(ref _packageName, value, StringComparer.OrdinalIgnoreCase))
                {
                    UpdateVersions(value);
                    FindPackage(value, SelectedPackageVersion);
                }
            }
        }

        private async void UpdateVersions(string name)
        {
            string[] latest = new[] {Properties.Resources.LatestVersion};
            if (string.IsNullOrWhiteSpace(name))
            {
                PackageVersions = latest;
            }

            IEnumerable<string> versions = await VSPackage.Manager.Provider.GetVersionsAsync(name) ?? latest;
            PackageVersions = versions.ToList();

            foreach (string version in PackageVersions)
            {
                Version v;
                if (Version.TryParse(version, out v))
                {
                    SelectedPackageVersion = version;
                    return;
                }
            }

            SelectedPackageVersion = PackageVersions.FirstOrDefault();
        }

        public IReadOnlyList<string> PackageVersions
        {
            get { return _packageVersions; }
            set { Set(ref _packageVersions, value); }
        }

        public string RootFolderName
        {
            get { return _rootFolderName; }
            set { Set(ref _rootFolderName, value, StringComparer.Ordinal); }
        }

        public HashSet<string> SelectedFiles { get; private set; }

        public string SelectedPackageVersion
        {
            get { return _selectedPackageVersion; }
            set
            {
                if (Set(ref _selectedPackageVersion, value, StringComparer.Ordinal))
                {
                    FindPackage(PackageName, value);
                }
            }
        }

        public bool IsPackageListLoaded
        {
            get { return _isPackageListLoaded; }
            set { Set(ref _isPackageListLoaded, value); }
        }

        private async void FindPackage(string packageName, string version)
        {
            if (string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(version))
            {
                Package = null;
            }
            else
            {
                Package = await VSPackage.Manager.Provider.GetInstallablePackage(packageName, version);
            }
        }

        private void InstallPackage()
        {
            Package.Files = SelectedFiles;
            VSPackage.Options.CreatePackageFolder = IncludePackageName;
            VSPackage.Options.SaveSettingsToStorage();
            _closeDialog(true);
        }

        private bool PackageIsSet()
        {
            return Package != null;
        }

        private void RebuildPackageTree(InstallablePackage package)
        {
            if (package != Package)
            {
                return;
            }

            DisplayRoots = null;

            if (package == null)
            {
                return;
            }

            PackageItem root = new PackageItem(this)
            {
                ItemType = PackageItemType.Folder,
                Name = RootFolderName,
                IsExpanded = true
            };

            PackageItem topLevelChildrenParent = root;

            if (IncludePackageName)
            {
                topLevelChildrenParent = new PackageItem(this)
                {
                    Name = package.Name,
                    ItemType = PackageItemType.Folder,
                    IsExpanded = true
                };

                root.Children = new[] {topLevelChildrenParent};
            }

            foreach (string file in package.Files)
            {
                string[] parts = file.Split('/');
                PackageItem currentParent = topLevelChildrenParent;

                for (int i = 0; i < parts.Length; ++i)
                {
                    bool isFolder = i != parts.Length - 1;

                    if (isFolder)
                    {
                        PackageItem next = currentParent.Children.FirstOrDefault(x => x.ItemType == PackageItemType.Folder && string.Equals(x.Name, parts[i]));

                        if (next == null)
                        {
                            next = new PackageItem(this)
                            {
                                Name = parts[i],
                                ItemType = PackageItemType.Folder,
                                IsExpanded = true
                            };

                            List<PackageItem> children = new List<PackageItem>(currentParent.Children)
                            {
                                next
                            };

                            children.Sort((x, y) => x.ItemType == y.ItemType ? StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name) : y.ItemType == PackageItemType.Folder ? 1 : -1);

                            currentParent.Children = children;
                        }

                        currentParent = next;
                    }
                    else
                    {
                        PackageItem next = new PackageItem(this)
                        {
                            FullPath = file,
                            Name = parts[i],
                            ItemType = PackageItemType.File,
                            IsExpanded = true,
                            IsMain = string.Equals(package.MainFile, file, StringComparison.OrdinalIgnoreCase),
                        };

                        List<PackageItem> children = new List<PackageItem>(currentParent.Children)
                        {
                            next
                        };

                        children.Sort((x, y) => x.ItemType == y.ItemType ? StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name) : y.ItemType == PackageItemType.Folder ? -1 : 1);

                        currentParent.Children = children;
                    }
                }
            }

            if (package == Package)
            {
                Dispatcher.Invoke(() => DisplayRoots = new[] {root});
            }
        }
    }

    internal class SlashCountComparer : IComparer<string>
    {
        public static IComparer<string> Instance { get; } = new SlashCountComparer();

        public int Compare(string left, string right)
        {
            return -left.Count(x => x == '/').CompareTo(right.Count(x => x == '/'));
        }
    }
}
