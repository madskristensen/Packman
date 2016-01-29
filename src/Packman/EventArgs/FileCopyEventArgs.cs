using System;

namespace Packman
{
    public class FileCopyEventArgs : EventArgs
    {
        public FileCopyEventArgs(string source, string destination)
        {
            Source = source;
            Destination = destination;
        }

        public string Source { get; set; }
        public string Destination { get; set; }
    }
}
