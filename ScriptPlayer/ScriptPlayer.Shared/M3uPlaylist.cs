using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ScriptPlayer.Shared
{
    public class M3uPlaylist
    {
        private static readonly Regex ExtInfMarker = new Regex(@"^\#EXTINF\:(?<Duration>\-?\d+)\,(?<Name>.*)$", RegexOptions.Compiled);

        public class M3uEntry
        {
            public string FilePath { get; set; }
            public string DisplayName { get; set; }
            public int DurationInSeconds { get; set; }
        }

        /// <summary>
        /// #EXTM3U
        /// #EXTINF:123, Sample artist - Sample title
        /// C:\Documents and Settings\I\My Music\Sample.mp3
        ///
        /// #EXTINF:321,Example Artist - Example title
        /// C:\Documents and Settings\I\My Music\Greatest Hits\Example.ogg
        /// </summary>

        public List<M3uEntry> Entries = new List<M3uEntry>();

        public void Save(string filename)
        {
            string directory = Path.GetDirectoryName(filename);

            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException(@"Directory is null", nameof(filename));

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                Save(stream);
        }

        public void Save(Stream stream)
        {
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.WriteLine("#EXTM3U");
                writer.WriteLine();

                foreach (M3uEntry entry in Entries)
                {
                    writer.WriteLine($"#EXTINF:{entry.DurationInSeconds},{entry.DisplayName}");
                    writer.WriteLine(entry.FilePath);
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

            string previousDisplayName = null;
            int previousDuration = 0;

            using (StreamReader reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine()?.Trim();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (line.StartsWith("#"))
                    {
                        if (ExtInfMarker.IsMatch(line))
                        {
                            Match match = ExtInfMarker.Match(line);
                            previousDisplayName = match.Groups["Name"].Value;

                            int duration;
                            if (int.TryParse(match.Groups["Duration"].Value, out duration))
                                previousDuration = duration;


                        }
                        continue;
                    }

                    string filePath = line.Trim();

                    M3uEntry entry = new M3uEntry
                    {
                        DisplayName = previousDisplayName ?? Path.GetFileNameWithoutExtension(filePath),
                        DurationInSeconds = previousDuration,
                        FilePath = filePath
                    };

                    previousDisplayName = null;
                    previousDuration = 0;

                    Entries.Add(entry);
                }
            }
        }

        public static M3uPlaylist FromFile(string filename)
        {
            try
            {
                if (!File.Exists(filename))
                    return null;

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
