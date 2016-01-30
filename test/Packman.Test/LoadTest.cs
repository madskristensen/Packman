using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Packman.Test
{
    [TestClass]
    public class LoadTest
    {
        IEnumerable<string> _packages;

        [TestInitialize]
        public void Initialize()
        {
            _packages = AssemblyLoad.Api.GetPackageNamesAsync().Result;
        }

        [TestMethod, TestCategory("Load")]
        public void ReturnPackages()
        {
            Assert.IsTrue(_packages.Count() > 1700, "Packages didn't load");
        }

        [TestMethod, TestCategory("Load")]
        public async Task AssetHasVersions()
        {
            var angular = await AssemblyLoad.Api.GetInstallablePackage("jquery", "2.2.0");
            Assert.AreEqual(3, angular.Files.Count(), "jquery assets not correct");
        }
    }
}
