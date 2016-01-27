using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using Packman;
using Microsoft.VisualStudio.Shell;
using System.Windows.Interop;

namespace PackmanVsix
{
    internal sealed class InstallPackageCommand
    {
        private readonly Package _package;

        private InstallPackageCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            _package = package;

            var service = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (service != null)
            {
                var cmdId = new CommandID(PackageGuids.guidLibrarianCmdSet, PackageIds.InstallLibrary);
                var button = new OleMenuCommand(Install, cmdId);
                button.BeforeQueryStatus += BeforeQueryStatus;
                service.AddCommand(button);
            }
        }

        public static InstallPackageCommand Instance { get; private set; }

        IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package)
        {
            Instance = new InstallPackageCommand(package);
        }

        void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            var item = ProjectHelpers.GetSelectedItems().FirstOrDefault();
            button.Visible = item != null && item.Kind == Constants.vsProjectItemKindPhysicalFolder;
        }

        async void Install(object sender, EventArgs e)
        {
            var package = await GetPackage();

            if (package == null)
                return;

            var item = ProjectHelpers.GetSelectedItems().FirstOrDefault();
            string manifestPath = item.ContainingProject.GetConfigFile();

            var settings = new InstallSettings
            {
                InstallDirectory = Path.Combine(item.GetFullPath(), package.Name),
                SaveManifest = true,
                OnlyMainFile = false
            };

            await VSPackage.Manager.Install(manifestPath, package, settings);

            if (settings.SaveManifest)
                item.ContainingProject.AddFileToProject(manifestPath, "None");
        }

        private async Task<InstallablePackage> GetPackage(string folder)
        {
            InstallDialog dialog = new InstallDialog(folder);

            var hwnd = new IntPtr(VSPackage.DTE.MainWindow.HWnd);
            System.Windows.Window window = (System.Windows.Window)HwndSource.FromHwnd(hwnd).RootVisual;
            dialog.Owner = window;

            var result = dialog.ShowDialog();

            if (!result.HasValue || !result.Value)
                return null;

            string name = dialog.PackageName;

            return await VSPackage.Manager.Provider.GetInstallablePackage(name, dialog.PackageVersion);
        }
    }
}
