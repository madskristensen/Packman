using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System;

namespace Packman
{
    public class InstallablePackage
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string MainFile { get; set; }
        public IEnumerable<string> Files { get; set; }
        public IEnumerable<string> AllFiles { get; set; }
        internal string UrlFormat { get; set; }

        public bool AreAllFilesRecommended()
        {
            var extensions = new List<string>();
            string[] ignore = { ".map" };

            foreach (string file in Files)
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();

                if (!ignore.Contains(ext) && !extensions.Contains(ext))
                    extensions.Add(ext);

                if (extensions.Count >= 2)
                    return true;
            }

            return false;
        }

        internal async Task DownloadFiles(string downloadDir)
        {
            if (Directory.Exists(downloadDir))
                return;

            var list = new List<Task>();

            foreach (string fileName in AllFiles)
            {
                string url = string.Format(UrlFormat, Name, Version, fileName);
                var localFile = new FileInfo(Path.Combine(downloadDir, fileName));

                localFile.Directory.Create();

                using (WebClient client = new WebClient())
                {
                    var task = client.DownloadFileTaskAsync(url, localFile.FullName);
                    list.Add(task);
                }
            }

            OnDownloading(downloadDir);
            await Task.WhenAll(list);
            OnDownloaded(downloadDir);
        }

        void OnDownloading(string path)
        {
            if (Downloading != null)
                Downloading(this, new InstallEventArgs(this, path));
        }

        void OnDownloaded(string path)
        {
            if (Downloaded != null)
                Downloaded(this, new InstallEventArgs(this, path));
        }

        public static event EventHandler<InstallEventArgs> Downloading;
        public static event EventHandler<InstallEventArgs> Downloaded;
    }
}
