using System.Threading.Tasks;
using System.Linq;
using System.Windows;

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

        public string PackageName
        {
            get { return cbName.Text.Trim(); }
        }

        public string PackageVersion
        {
            get { return cbVersion.Text; }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Title = VSPackage.Name;

            cbName.Focus();
            cbName.ItemsSource = await VSPackage.Manager.Provider.GetPackageNamesAsync();

            cbVersion.ItemsSource = new[] { LATEST };
            cbVersion.GotFocus += OnVersionGotFocus;

            cbName.LostFocus += async delegate { await ShowFiles(); };
            cbName.SelectionChanged += async delegate { await ShowFiles(); };
            cbVersion.SelectionChanged += async delegate { await ShowFiles(); };
        }

        private async Task ShowFiles()
        {
            if (PackageVersion == LATEST || !cbName.ItemsSource.Cast<string>().Contains(PackageName))
                return;

            var package = await VSPackage.Manager.Provider.GetInstallablePackage(PackageName, PackageVersion);

            if (package != null)
            {
                treeView.ItemsSource = package.Files;
            }
        }

        private async void OnVersionGotFocus(object sender, RoutedEventArgs e)
        {
            string name = cbName.Text.Trim();
            var versions = await VSPackage.Manager.Provider.GetVersionsAsync(name);

            cbVersion.ItemsSource = versions != null ? versions : new[] { LATEST };
        }

        private void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
