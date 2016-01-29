using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Packman;
using System.Collections.Generic;
using System;
using System.Windows.Media.Imaging;

namespace PackmanVsix
{
    public partial class InstallDialog : Window
    {
        private string _folder;
        private const string LATEST = "Latest version";
        private TreeViewItem _parentItem;

        public InstallDialog(string folder)
        {
            InitializeComponent();

            _folder = folder;

            Loaded += OnLoaded;
        }

        public InstallablePackage Package { get; private set; }

        public string InstallDirectory
        {
            get
            {
                return cbCreateFolder.IsChecked.Value ? cbName.Text.Trim() : string.Empty;
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Icon = BitmapFrame.Create(new Uri("pack://application:,,,/PackmanVsix;component/Resources/dialog-icon.png", UriKind.RelativeOrAbsolute));
            Title = VSPackage.Name;

            cbName.Focus();
            cbName.ItemsSource = await VSPackage.Manager.Provider.GetPackageNamesAsync();

            if (cbName.ItemsSource == null)
            {
                cbName.Text = "Packages could not be loaded";
                cbName.IsEnabled = false;
                btnInstall.IsEnabled = false;
            }

            cbVersion.ItemsSource = new[] { LATEST };
            cbVersion.GotFocus += OnVersionGotFocus;

            cbName.SelectionChanged += async delegate { await ShowFiles(); };
            cbVersion.SelectionChanged += async delegate { await ShowFiles(); };

            cbCreateFolder.IsChecked = VSPackage.Options.CreatePackageFolder;
            cbCreateFolder.Checked += CreateFolderToggle;
            cbCreateFolder.Unchecked += CreateFolderToggle;
        }

        private void CreateFolderToggle(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox)sender;
            if (_parentItem != null)
            {
                var header = (CheckBox)_parentItem.Header;

                if (box.IsChecked.Value)
                    header.Content = $"{_folder}/{cbName.Text.Trim()}";
                else
                    header.Content = _folder;
            }
        }

        private async Task ShowFiles()
        {
            string name = cbName.Text.Trim();

            if (!cbName.ItemsSource.Cast<string>().Contains(name))
                return;

            var versionList = await VSPackage.Manager.Provider.GetVersionsAsync(name);
            var version = (string)cbVersion.SelectedItem;

            if (versionList == null || !versionList.Contains(version))
                return;

            var package = await VSPackage.Manager.Provider.GetInstallablePackage(name, version);

            if (package == null)
                return;

            bool isChecked = package.AreAllFilesRecommended();
            string itemName = _folder;

            if (cbCreateFolder.IsChecked.HasValue && cbCreateFolder.IsChecked.Value)
                itemName += $"/{itemName}";

            var masterCb = new CheckBox { Content = itemName, IsChecked = isChecked };

            if (package.Files.Count() == 1)
            {
                masterCb.IsChecked = true;
                masterCb.IsEnabled = false;
            }

            _parentItem = new TreeViewItem { IsExpanded = true };
            _parentItem.Header = masterCb;
            masterCb.Checked += delegate { ToggleChecked(true); };
            masterCb.Unchecked += delegate { ToggleChecked(false); };

            foreach (string file in package.Files)
            {
                bool isMain = package.MainFile == file;

                var item = new TreeViewItem
                {
                    Header = new CheckBox
                    {
                        Content = file,
                        IsChecked = isMain || isChecked,
                        IsEnabled = !isMain,
                        FontWeight = isMain ? FontWeights.Bold : FontWeights.Normal,
                        ToolTip = isMain ? "The main file for this package" : null
                    }
                };

                _parentItem.Items.Add(item);
            }

            treeView.Items.Clear();
            treeView.Items.Add(_parentItem);
        }

        private void ToggleChecked(bool check)
        {
            var first = (TreeViewItem)treeView.Items[0];

            foreach (TreeViewItem item in first.Items)
            {
                var cb = (CheckBox)item.Header;

                if (cb.IsEnabled)
                    cb.IsChecked = check;
            }
        }

        private void SelectStableVersion(IEnumerable<string> versions)
        {
            int index = 0;

            for (int i = 0; i < versions.Count() - 1; i++)
            {
                string version = versions.ElementAt(i);
                Version v;

                if (Version.TryParse(version, out v))
                {
                    index = i;
                    break;
                }
            }

            cbVersion.SelectedIndex = index;
        }

        private IEnumerable<string> GetSelectedFiles()
        {
            var first = (TreeViewItem)treeView.Items[0];
            var list = new List<string>();

            foreach (TreeViewItem item in first.Items)
            {
                var cb = (CheckBox)item.Header;

                if (cb.IsChecked.HasValue && cb.IsChecked.Value)
                    list.Add((string)cb.Content);
            }

            return list;
        }

        private async void OnVersionGotFocus(object sender, RoutedEventArgs e)
        {
            string name = cbName.Text.Trim();
            var versions = await VSPackage.Manager.Provider.GetVersionsAsync(name) ?? new[] { LATEST };

            cbVersion.ItemsSource = versions;
            SelectStableVersion(versions);
        }

        private async void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            string name = cbName.Text.Trim();
            var version = (string)cbVersion.SelectedItem;

            Package = await VSPackage.Manager.Provider.GetInstallablePackage(name, version);
            Package.Files = GetSelectedFiles();

            VSPackage.Options.CreatePackageFolder = cbCreateFolder.IsChecked.Value;
            VSPackage.Options.SaveSettingsToStorage();

            DialogResult = true;
            Close();
        }
    }
}
