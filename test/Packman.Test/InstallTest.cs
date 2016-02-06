using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Packman.Test
{
    [TestClass]
    public class InstallTest
    {
        string _cwd, _manifestPath;
        IEnumerable<Manager> _managers;
        const string _manifestFileName = "packman.json";

        [TestInitialize]
        public void Initialize()
        {
            _cwd = new DirectoryInfo("..\\..\\test").FullName;
            Directory.CreateDirectory(_cwd);

            Defaults.CacheDays = 3;

            _manifestPath = Path.Combine(_cwd, _manifestFileName);

            IPackageProvider[] managers = { new JsDelivrProvider(), new CdnjsProvider() };
            _managers = managers.Select(a => new Manager(a));
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(_cwd, true);

            string artifactTests = "..\\..\\artifacts\\custom";
            if (Directory.Exists(artifactTests))
                Directory.Delete(artifactTests, true);
        }

        [TestMethod, TestCategory("Install")]
        public async Task InstallPackage()
        {
            foreach (var manager in _managers)
            {
                var entry = await manager.Provider.GetInstallablePackageAsync("jquery", "2.2.0");
                entry.Files = new[] { "jquery.js" };

                Assert.IsNotNull(entry.MainFile, "Main file is not set");
                string path = Path.Combine(_cwd, "js");
                await manager.Install(_manifestPath, entry, path);

                string config = Path.Combine(_cwd, _manifestFileName);

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
                var entry = await manager.Provider.GetInstallablePackageAsync("knockout", "3.4.0");
                string path = Path.Combine(_cwd, "js/lib");
                await manager.Install(_manifestPath, entry, path);

                string config = Path.Combine(_cwd, _manifestFileName);
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
                var entry = await manager.Provider.GetInstallablePackageAsync("16pixels", "0.1.6");
                string path = Path.Combine(_cwd, "lib");
                await manager.Install(_manifestPath, entry, path, false);

                string config = Path.Combine(_cwd, _manifestFileName);

                if (File.Exists(config))
                {
                    string content = File.ReadAllText(config);
                    Assert.IsFalse(content.Contains("16pixels"));
                }
            }
        }

        [TestMethod, TestCategory("Install")]
        public async Task InstallCustomUrls()
        {
            string root = new DirectoryInfo("..\\..\\artifacts").FullName;
            string manifestFilePath = Path.Combine(root, "urlinstall.json");

            Manifest manifest = await Manifest.FromFileOrNewAsync(manifestFilePath);
            await _managers.First().InstallAll(manifest);

            string file = Path.Combine(root, "custom", "jquery.js");
            Assert.IsTrue(File.Exists(file));

        }
    }
}
