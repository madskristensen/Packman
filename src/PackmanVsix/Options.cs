using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace PackmanVsix
{
    public class Options : DialogPage
    {
        [DisplayName("Save manifest file")]
        [Category("General")]
        [Description("Creates and maintains the packman.json file at the root of the project.")]
        [DefaultValue(true)]
        public bool SaveManifestFile { get; set; } = true;

        [DisplayName("Add package folder")]
        [Category("General")]
        [Description("Creates a new folder for the package instead of installing the files directly into the selected folder.")]
        [DefaultValue(true)]
        public bool CreatePackageFolder { get; set; } = true;
    }
}
