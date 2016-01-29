using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Packman.Test
{
    [TestClass]
    public class ManagerTest
    {
        //IEnumerable<RemotePackage> _packages;
        string _cwd, _manifestPath;
        Manager manager;

        [TestInitialize]
        public void Initialize()
        {
            var guid = "test";
            _cwd = new DirectoryInfo("..\\..\\" + guid).FullName;
            Directory.CreateDirectory(_cwd);

            _manifestPath = Path.Combine(_cwd, Defaults.ManifestFileName);
            manager = new Manager(AssemblyLoad.Api);

            //_packages = AssemblyLoad.Api.GetPackagesAsync().Result;
        }

        [TestMethod, TestCategory("Install")]
        public async Task InstallPackage()
        {
            var entry = await AssemblyLoad.Api.GetInstallablePackage("jquery", "2.2.0");
            string path = Path.Combine(_cwd, "js");
            await manager.Install(_manifestPath, entry, path);

            string config = Path.Combine(_cwd, Defaults.ManifestFileName);

            Assert.IsTrue(File.Exists(config), "Config not created");

            string file = Path.Combine(path, "jquery.js");
            Assert.IsTrue(File.Exists(file), "Remote file not copied");
        }

        [TestMethod, TestCategory("Install")]
        public async Task InstallPackageWithCustomPath()
        {
            var entry = await AssemblyLoad.Api.GetInstallablePackage("angularjs", "1.4.7");
            string path = Path.Combine(_cwd, "js/lib");
            await manager.Install(_manifestPath, entry, path);

            string config = Path.Combine(_cwd, Defaults.ManifestFileName);
            string content = File.ReadAllText(config);

            Assert.IsTrue(File.Exists(config), "Config not created");
            Assert.IsTrue(content.Contains("\"path\": \"js/lib\""));
        }

        [TestMethod, TestCategory("Install")]
        public async Task InstallPackageDontSaveManifest()
        {
            var entry = await AssemblyLoad.Api.GetInstallablePackage("jquery.ui", "1.11.4");
            string path = Path.Combine(_cwd, "lib");
            await manager.Install(_manifestPath, entry, path, false);

            string config = Path.Combine(_cwd, Defaults.ManifestFileName);

            if (File.Exists(config))
            {
                string content = File.ReadAllText(config);
                Assert.IsFalse(content.Contains("jquery.ui"));
            }
        }

        //[TestMethod, TestCategory("Install")]
        //public async Task ClearCache()
        //{
        //    await Task.Delay(3000);

        //    manager.ClearCache();
        //    string rootCacheDir = Environment.ExpandEnvironmentVariables(Defaults.CachePath);
        //    string dir = Path.Combine(rootCacheDir, manager.Provider.Name);

        //    Assert.IsFalse(Directory.Exists(dir), "Didnt' clear the cache");
        //}
    }
}
