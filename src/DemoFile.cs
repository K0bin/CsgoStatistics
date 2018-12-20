using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CsgoStatistics
{
    public class DemoFile : IDisposable
    {
        public Stream Stream
        {
            get; private set;
        }

        public string FileName
        {
            get; private set;
        }

        public DateTime LastModified
        {
            get; private set;
        }

        public DemoFile(string path)
        {
            this.FileName = Path.GetFileName(path);
            this.LastModified = File.GetLastWriteTime(path);
            this.Stream = new MemoryStream(File.ReadAllBytes(path));
        }

        public void Dispose()
        {
            this.Stream.Dispose();
        }
    }
}
