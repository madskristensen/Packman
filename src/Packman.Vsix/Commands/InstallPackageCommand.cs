using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using Packman;
using Microsoft.VisualStudio.Shell;

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

        private async Task<InstallablePackage> GetPackage()
        {
            var prompt = new Form
            {
                Width = 500,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = VSPackage.Name,
                StartPosition = FormStartPosition.CenterScreen
            };
            var textLabel = new Label { Left = 50, Top = 20, Text = "Name of the package" };
            var textBox = new TextBox { Left = 50, Top = 50, Width = 400 };
            var confirmation = new Button { Text = "Ok", Left = 350, Width = 100, Top = 70, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            string name = prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";

            if (!string.IsNullOrEmpty(name))
            {
                var versions = await VSPackage.Manager.Provider.GetVersionsAsync(name);

                if (versions != null)
                    return await VSPackage.Manager.Provider.GetInstallablePackage(name, versions.First());
            }

            return null;
        }
    }
}
