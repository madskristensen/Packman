using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Packman;
using System.Collections.Generic;

namespace PackmanVsix
{
    public partial class InstallDialog : Window
    {
        private string _folder;
        private const string LATEST = "Latest version";

        public InstallDialog(string folder)
        {
            InitializeComponent();

            _folder = folder;

            Loaded += OnLoaded;
        }

        public InstallablePackage Package { get; private set; }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Title = VSPackage.Name;

            cbName.Focus();
            cbName.ItemsSource = await VSPackage.Manager.Provider.GetPackageNamesAsync();

            cbVersion.ItemsSource = new[] { LATEST };
            cbVersion.GotFocus += OnVersionGotFocus;

            cbName.SelectionChanged += async delegate { await ShowFiles(); };
            cbVersion.SelectionChanged += async delegate { await ShowFiles(); };
        }

        private async Task ShowFiles()
        {
            string name = cbName.Text.Trim();

            if (!cbName.ItemsSource.Cast<string>().Contains(name))
                return;

            var versionList = await VSPackage.Manager.Provider.GetVersionsAsync(name);
            string version = (string)cbVersion.SelectedItem;

            if (versionList == null || !versionList.Contains(version))
                return;

            var package = await VSPackage.Manager.Provider.GetInstallablePackage(name, version);

            if (package == null)
                return;

            bool isChecked = package.AreAllFilesRecommended();

            CheckBox masterCb = new CheckBox { Content = _folder, IsChecked = isChecked };
            var folderItem = new TreeViewItem { IsExpanded = true };
            folderItem.Header = masterCb;
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

                folderItem.Items.Add(item);
            }

            treeView.Items.Clear();
            treeView.Items.Add(folderItem);
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
            var versions = await VSPackage.Manager.Provider.GetVersionsAsync(name);

            cbVersion.ItemsSource = versions ?? new[] { LATEST };
            cbVersion.SelectedIndex = 0;
        }

        private async void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            string name = cbName.Text.Trim();
            string version = (string)cbVersion.SelectedItem;

            Package = await VSPackage.Manager.Provider.GetInstallablePackage(name, version);
            Package.Files = GetSelectedFiles();

            DialogResult = true;
            Close();
        }
    }
}
