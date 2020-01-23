using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace ScriptPlayer.Shared.Scripts
{
    public abstract class ScriptLoader
    {
        public long MaxFileSize { get; set; } = 1024 * 1024 * 128; // 128 MB

        public static CultureInfo Culture = CultureInfo.InvariantCulture;

        public abstract List<ScriptAction> Load(Stream stream);

        public abstract List<ScriptFileFormat> GetSupportedFormats();

        public List<ScriptAction> Load(string filename)
        {
            WaitForFileReadable(filename);
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                return Load(stream);
        }

        private void WaitForFileReadable(string filename)
        {
            for(int i = 0; i < 5; i++)
            {
                try
                {
                    using (File.OpenRead(filename));
                }
                catch (System.IO.IOException ex)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(50));
                }
            }
        }
    }
}
