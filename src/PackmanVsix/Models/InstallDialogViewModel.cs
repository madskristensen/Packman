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
        private IReadOnlyList<string> _availablePacakges;
        private IReadOnlyList<PackageItem> _displayRoot;
        private bool _includePackageName;
        private bool _isPackageListLoaded;
        private InstallablePackage _package;
        private string _packageName;
        private IReadOnlyList<string> _packageVersions;
        private string _rootFolderName;
        private string _selectedPackageVersion;

        public InstallDialogViewModel(Dispatcher dispatcher, Action<bool> closeDialog)
        {
            Dispatcher = dispatcher;
            _closeDialog = closeDialog;
            IncludePackageName = VSPackage.Options.CreatePackageFolder;
            SelectedFiles = null;
            InstallPackageCommand = ActionCommand.Create(InstallPackage, CanInstallPackage, false);
            LoadPackages();
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
                            PackageItem packageItem = new PackageItem(this, SelectedFiles)
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

        public bool IsPackageListLoaded
        {
            get { return _isPackageListLoaded; }
            set { Set(ref _isPackageListLoaded, value); }
        }

        public InstallablePackage Package
        {
            get { return _package; }
            set
            {
                if (Set(ref _package, value))
                {
                    SelectedFiles = null;
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

        private bool CanInstallPackage()
        {
            return Package != null && SelectedFiles != null && SelectedFiles.Count > 0;
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

            bool canUpdateInstallStatusValue = false;
            Func<bool> canUpdateInstallStatus = () => canUpdateInstallStatusValue;
            HashSet<string> selectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            PackageItem root = new PackageItem(this, selectedFiles)
            {
                CanUpdateInstallStatus = canUpdateInstallStatus,
                ItemType = PackageItemType.Folder,
                Name = RootFolderName
            };

            PackageItem topLevelChildrenParent = root;

            if (IncludePackageName)
            {
                topLevelChildrenParent = new PackageItem(this, selectedFiles)
                {
                    CanUpdateInstallStatus = canUpdateInstallStatus,
                    Name = package.Name,
                    ItemType = PackageItemType.Folder
                };

                root.Children = new[] { topLevelChildrenParent };
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
                            next = new PackageItem(this, selectedFiles)
                            {
                                CanUpdateInstallStatus = canUpdateInstallStatus,
                                Name = parts[i],
                                ItemType = PackageItemType.Folder
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
                        PackageItem next = new PackageItem(this, selectedFiles)
                        {
                            CanUpdateInstallStatus = canUpdateInstallStatus,
                            FullPath = file,
                            Name = parts[i],
                            ItemType = PackageItemType.File,
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

            SetNodeOpenStates(root);

            Dispatcher.Invoke(() =>
            {
                if (package == Package)
                {
                    canUpdateInstallStatusValue = true;
                    DisplayRoots = new[] {root};
                    SelectedFiles = selectedFiles;
                    InstallPackageCommand.CanExecute(null);
                }
            });
        }

        private static void SetNodeOpenStates(PackageItem item)
        {
            bool shouldBeOpen = false;

            foreach (PackageItem child in item.Children)
            {
                SetNodeOpenStates(child);
                shouldBeOpen |= child.IsChecked || child.IsExpanded;
            }

            item.IsExpanded = shouldBeOpen;
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
    }
}
