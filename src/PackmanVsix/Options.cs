using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace PackmanVsix
{
    public class Options : DialogPage
    {
        [DisplayName("Save manifest file")]
        [Category("Package install")]
        [Description("Creates and maintains the packman.json file at the root of the project.")]
        [DefaultValue(false)]
        public bool SaveManifestFile { get; set; }

        [DisplayName("Restore packages on save")]
        [Category("Package install")]
        [Description("Automatically restore packages when saving packman.json.")]
        [DefaultValue(true)]
        public bool RestoreOnSave { get; set; } = true;

        [DisplayName("Add package folder")]
        [Category("Package install")]
        [Description("Creates a new folder for the package instead of installing the files directly into the selected folder.")]
        [DefaultValue(true)]
        public bool CreatePackageFolder { get; set; } = true;

        [DisplayName("Cache folder")]
        [Category("General")]
        [Description("The location of the cache. It could be a Dropbox/OneDrive folder if you want the cache to roam. Environment variables allowed.")]
        [DefaultValue("%userprofile%\\.packman")]
        public string CachePath { get; set; } = "%userprofile%\\.packman";

        [DisplayName("Days between update checks")]
        [Category("General")]
        [Description("The number of days between checking for updates to the registry.")]
        [DefaultValue(1)]
        public int CacheDays { get; set; } = 1;

        // TODO: get rid of the enum and make this dynamic and MEF'ed out
        [DisplayName("Provider")]
        [Category("Provider")]
        [Description("The CDN that provides the data.")]
        [DefaultValue(Providers.Cdnjs)]
        [TypeConverter(typeof(EnumConverter))]
        public Providers Provider { get; set; } = Providers.Cdnjs;


        protected override void OnApply(PageApplyEventArgs e)
        {
            // Clean up the path
            CachePath = CachePath.Replace("/", "\\");

            if (string.IsNullOrEmpty(CachePath))
                e.ApplyBehavior = ApplyKind.Cancel;
            else
                base.OnApply(e);
        }

        public override void SaveSettingsToStorage()
        {
            base.SaveSettingsToStorage();

            if (Saved != null)
            {
                Saved(this, EventArgs.Empty);
            }
        }

        public event EventHandler<EventArgs> Saved;
    }

    // TODO: get rid of the enum and make this dynamic and MEF'ed out
    public enum Providers
    {
        JsDelivr,
        Cdnjs
    }
}
