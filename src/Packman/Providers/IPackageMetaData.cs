using System.Collections.Generic;

namespace Packman
{
    public interface IPackageMetaData
    {
        IEnumerable<JsDelivrAsset> Assets { get; set; }
        string Author { get; set; }
        string Description { get; set; }
        string GitHub { get; set; }
        string Homepage { get; set; }
        string LastVersion { get; set; }
        string MainFile { get; set; }
        string Name { get; set; }
    }
}