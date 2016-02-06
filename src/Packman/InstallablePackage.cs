using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System;

namespace Packman
{
    public class InstallablePackage : IInstallablePackage
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string MainFile { get; set; }
        public IEnumerable<string> Files { get; set; }
        public IEnumerable<string> AllFiles { get; set; }
        internal string UrlFormat { get; set; }

        public int TotalFileCount
        {
            get { return AllFiles.Count(); }
        }

        //public bool AreAllFilesRecommended()
        //{
        //    var extensions = new List<string>();
        //    string[] ignore = { ".map" };

        //    foreach (string file in Files)
        //    {
        //        string ext = Path.GetExtension(file).ToLowerInvariant();

        //        if (!ignore.Contains(ext) && !extensions.Contains(ext))
        //            extensions.Add(ext);

        //        if (extensions.Count >= 2)
        //            return true;
        //    }

        //    return false;
        //}

        internal async Task DownloadFilesAsync(string downloadDir)
        {
            if (Directory.Exists(downloadDir))
                return;

            var list = DownloadFiles(downloadDir, Files);

            OnDownloading(downloadDir);
            await Task.WhenAll(list);

            var remaining = AllFiles.Where(f => !Files.Contains(f));

            if (remaining.Any())
            {
                // Fire and forget to return call immediately. Download the rest of the files
                // TODO: Is there a more elegant way than using the threadpool like this?
                System.Threading.ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    OnDownloadingRemainingFiles(downloadDir);

                    await Task.WhenAll(DownloadFiles(downloadDir, remaining));
                });
            }
        }

        List<Task> DownloadFiles(string downloadDir, IEnumerable<string> files)
        {
            var list = new List<Task>();

            foreach (string fileName in files)
            {
                try
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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write(ex);
                }
            }

            return list;
        }

        void OnDownloading(string path)
        {
            if (Downloading != null)
                Downloading(this, new InstallEventArgs(this, path));
        }

        void OnDownloadingRemainingFiles(string path)
        {
            if (DownloadingRemainingFiles != null)
                DownloadingRemainingFiles(this, new InstallEventArgs(this, path));
        }

        public static event EventHandler<InstallEventArgs> Downloading;
        public static event EventHandler<InstallEventArgs> DownloadingRemainingFiles;
    }
}
