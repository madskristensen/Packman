using System.Windows.Media;

namespace Packman.Providers
{
    public interface IPackageInfo
    {
        string Name { get; }

        string Description { get; }

        string Homepage { get; }

        ImageSource Icon { get; }
    }
}
