using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Packman.Test
{
    [TestClass]
    public class ManagerTest
    {
        string _cwd, _manifestPath;
        IEnumerable<Manager> _managers;

        [TestInitialize]
        public void Initialize()
        {
            var guid = "test";
            _cwd = new DirectoryInfo("..\\..\\" + guid).FullName;
            Directory.CreateDirectory(_cwd);

            _manifestPath = Path.Combine(_cwd, Defaults.ManifestFileName);
            _managers = AssemblyLoad.Apis.Select(a => new Manager(a));
        }

        [TestMethod, TestCategory("Install")]
        public async Task InstallPackage()
        {
            foreach (var manager in _managers)
            {
                var entry = await manager.Provider.GetInstallablePackage("jquery", "2.2.0");

                // TODO: fill the MainFile property on the Cdnjs provider
                //Assert.IsNotNull(entry.MainFile, "Main file is not set");
                string path = Path.Combine(_cwd, "js");
                await manager.Install(_manifestPath, entry, path);

                string config = Path.Combine(_cwd, Defaults.ManifestFileName);

                Assert.IsTrue(File.Exists(config), "Config not created");

                string file = Path.Combine(path, "jquery.js");
                Assert.IsTrue(File.Exists(file), "Remote file not copied");
            }
        }

        [TestMethod, TestCategory("Install")]
        public async Task InstallPackageWithCustomPath()
        {
            foreach (var manager in _managers)
            {
                var entry = await manager.Provider.GetInstallablePackage("knockout", "3.4.0");
                string path = Path.Combine(_cwd, "js/lib");
                await manager.Install(_manifestPath, entry, path);

                string config = Path.Combine(_cwd, Defaults.ManifestFileName);
                string content = File.ReadAllText(config);

                Assert.IsTrue(File.Exists(config), "Config not created");
                Assert.IsTrue(content.Contains("\"path\": \"js/lib\""));
            }
        }

        [TestMethod, TestCategory("Install")]
        public async Task InstallPackageDontSaveManifest()
        {
            foreach (var manager in _managers)
            {
                var entry = await manager.Provider.GetInstallablePackage("16pixels", "0.1.6");
                string path = Path.Combine(_cwd, "lib");
                await manager.Install(_manifestPath, entry, path, false);

                string config = Path.Combine(_cwd, Defaults.ManifestFileName);

                if (File.Exists(config))
                {
                    string content = File.ReadAllText(config);
                    Assert.IsFalse(content.Contains("16pixels"));
                }
            }
        }
    }
}
