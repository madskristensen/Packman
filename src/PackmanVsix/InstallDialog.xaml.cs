using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PackmanVsix
{
    public partial class InstallDialog : Window
    {
        private string _folder;

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

            cbVersion.GotFocus += OnVersionGotFocus;
        }

        private async void OnVersionGotFocus(object sender, RoutedEventArgs e)
        {
            string name = cbName.Text.Trim();
            var versions = await VSPackage.Manager.Provider.GetVersionsAsync(name);

            if (versions != null)
            {
                cbVersion.ItemsSource = versions;
            }
            else
            {
                cbVersion.Text = "Select version";
            }

            cbVersion.IsEnabled = versions != null;
        }
    }
}
