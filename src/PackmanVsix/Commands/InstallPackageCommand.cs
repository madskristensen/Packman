using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using EnvDTE;
using Packman;
using Microsoft.VisualStudio.Shell;
using System.Windows.Interop;
using System.Collections.Generic;

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
            var item = ProjectHelpers.GetSelectedItems().FirstOrDefault();

            if (item == null)
                return;

            var dir = new DirectoryInfo(item.GetFullPath());
            string installDir;
            var package = GetPackage(dir.Name, out installDir);

            if (package == null || package.Files == null || !package.Files.Any())
                return;

            string manifestPath = item.ContainingProject.GetConfigFile();

            var settings = new InstallSettings
            {
                InstallDirectory = Path.Combine(item.GetFullPath(), installDir),
                SaveManifest = VSPackage.Options.SaveManifestFile
            };

            await VSPackage.Manager.Install(manifestPath, package, settings);

            var props = new Dictionary<string, string> {
                { "name", package.Name.ToLowerInvariant().Trim()}                ,
                { "version", package.Version}
            };

            Telemetry.TrackEvent("Package installed", props);

            if (settings.SaveManifest)
                item.ContainingProject.AddFileToProject(manifestPath, "None");
        }

        private InstallablePackage GetPackage(string folder, out string installDirectory)
        {
            var dialog = new InstallDialog(folder);

            var hwnd = new IntPtr(VSPackage.DTE.MainWindow.HWnd);
            var window = (System.Windows.Window)HwndSource.FromHwnd(hwnd).RootVisual;
            dialog.Owner = window;

            var result = dialog.ShowDialog();

            installDirectory = folder;

            if (!result.HasValue || !result.Value)
            {
                return null;
            }

            installDirectory = dialog.InstallDirectory;

            return dialog.Package;
        }
    }
}
