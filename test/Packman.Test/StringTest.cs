using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Packman.Test
{
    [TestClass]
    public class StringTest
    {
        static string[] list =
        {
            "angularjs",
            "angular.foo",
            "jquery.ui",
            "jquery.foo.js",
            "jquery"
        };

        [TestMethod, TestCategory("String")]
        public void JqueryBeforeJqueryUI()
        {
            var sorted = list.OrderBy(s => s, new PackageNameComparer()).ToList();
            Assert.IsTrue(sorted.IndexOf("jquery") < sorted.IndexOf("jquery.ui"));
        }

        [TestMethod, TestCategory("String")]
        public void AngularjsBeforeAngularFoo()
        {
            var sorted = list.OrderBy(s => s, new PackageNameComparer()).ToList();
            Assert.IsTrue(sorted.IndexOf("angularjs") < sorted.IndexOf("angular.foo"));
        }
    }
}
