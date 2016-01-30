using System;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Packman;

namespace PackmanVsix
{
    internal sealed class RestorePackagesCommand
    {
        private readonly Package _package;

        private RestorePackagesCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            _package = package;

            var service = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (service != null)
            {
                var cmdId = new CommandID(PackageGuids.guidLibrarianCmdSet, PackageIds.RestoreAll);
                var button = new OleMenuCommand(async (s, e) => { await Restore(s, e); }, cmdId);
                button.BeforeQueryStatus += BeforeQueryStatus;
                service.AddCommand(button);
            }
        }

        public static RestorePackagesCommand Instance { get; private set; }

        IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package)
        {
            Instance = new RestorePackagesCommand(package);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            var item = ProjectHelpers.GetSelectedItems().FirstOrDefault();
            button.Visible = item.IsConfigFile();
        }

        private async System.Threading.Tasks.Task Restore(object sender, EventArgs e)
        {
            var item = ProjectHelpers.GetSelectedItems().FirstOrDefault();

            if (item == null)
                return;

            try
            {
                var manifest = await Manifest.FromFileOrNewAsync(item.GetFullPath());
                await VSPackage.Manager.InstallAll(manifest);

                Telemetry.TrackEvent("Packages restored");
            }
            catch (PackageNotFoundException ex)
            {
                VSPackage.DTE.StatusBar.Text = string.Format(Properties.Resources.ExceptionLoadingPackages, ex.Name, ex.Version);
            }
            catch (Exception ex)
            {
                VSPackage.DTE.StatusBar.Text = Properties.Resources.ErrorRestoringPackages;
                Logger.Log(ex);
            }
        }
    }
}
