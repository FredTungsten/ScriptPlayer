using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptPlayer.Shared
{
    public class M3uPlaylist
    {

        /// <summary>
        /// #EXTM3U
        /// #EXTINF:123, Sample artist - Sample title
        /// C:\Documents and Settings\I\My Music\Sample.mp3
        ///
        /// #EXTINF:321,Example Artist - Example title
        /// C:\Documents and Settings\I\My Music\Greatest Hits\Example.ogg
        /// </summary>

        public List<string> Entries = new List<string>();

        public void Save(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                Save(stream);
        }
        public void Save(Stream stream)
        {
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.WriteLine("#EXTM3U");

                foreach (string entry in Entries)
                {
                    writer.WriteLine("#EXTINF:0," + Path.GetFileNameWithoutExtension(entry));
                    writer.WriteLine(entry);
                    writer.WriteLine();
                }
            }
        }

        public void Load(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                Load(stream);
        }

        public void Load(Stream stream)
        {
            Entries.Clear();
            bool previouswasMarker = false;
            using (StreamReader reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (previouswasMarker)
                    {
                        previouswasMarker = false;
                        Entries.Add(line.Trim());
                    }
                    else if (line.StartsWith("#EXTINF:"))
                    {
                        previouswasMarker = true;
                    }
                }
            }
        }

        public static M3uPlaylist FromFile(string filename)
        {
            try
            {
                M3uPlaylist playlist = new M3uPlaylist();
                playlist.Load(filename);
                return playlist;
            }
            catch
            {
                return null;
            }
        }

        public static M3uPlaylist FromStream(Stream stream)
        {
            try
            {
                M3uPlaylist playlist = new M3uPlaylist();
                playlist.Load(stream);
                return playlist;
            }
            catch
            {
                return null;
            }
        }
    }
}
