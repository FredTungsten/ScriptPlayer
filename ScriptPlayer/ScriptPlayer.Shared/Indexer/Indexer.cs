using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ScriptPlayer.Shared
{
    public static class ScriptDownloader
    {
        public static async Task<DirectoryEntry> FetchIndex(string user, string repo, string branch, string path)
        {
            string url = ToGitHubDdl(user, repo, branch, path);
            WebClient client = new WebClient();
            byte[] content = await client.DownloadDataTaskAsync(new Uri(url, UriKind.Absolute));
            MemoryStream stream = new MemoryStream(content);

            return DirectoryEntry.FromStream(stream);
        }

        public static string ToGitHubDdl(string user, string repo, string branch, string path)
        {
            List<string> urlParts = new List<string>
            {
                user,
                repo,
                branch
            };
            urlParts.AddRange(path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries));
            var encodedParts = urlParts; // No URL encode on github ... .Select(HttpUtility.UrlEncode);
            var encodedPath = string.Join("/", encodedParts);

            return $"https://raw.githubusercontent.com/{encodedPath}";
        }
    }

    public class FileEntry
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        public static FileEntry FromFile(string file)
        {
            return new FileEntry
            {
                Name = Path.GetFileName(file)
            };
        }
    }

    public class DirectoryEntry
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlArray("Directories")]
        [XmlArrayItem("Directory")]
        public List<DirectoryEntry> Directories { get; set; }

        [XmlArray("Files")]
        [XmlArrayItem("File")]
        public List<FileEntry> Files { get; set; }

        public static DirectoryEntry FromDirectory(string path, Predicate<string> fileMatcher = null, Predicate<string> directoryMatcher = null)
        {
            DirectoryEntry result =
                new DirectoryEntry {Name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar))};

            foreach (string directory in Directory.GetDirectories(path))
            {
                if (directoryMatcher != null)
                {
                    if (!directoryMatcher(directory))
                        continue;
                }

                if (result.Directories == null)
                    result.Directories = new List<DirectoryEntry>();

                result.Directories.Add(FromDirectory(directory, fileMatcher, directoryMatcher));
            }

            foreach (string file in Directory.GetFiles(path))
            {
                if (fileMatcher != null)
                {
                    if (!fileMatcher(file))
                        continue;
                }

                if (result.Files == null)
                    result.Files = new List<FileEntry>();

                result.Files.Add(FileEntry.FromFile(file));
            }

            return result;
        }

        public static DirectoryEntry FromStream(Stream stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(DirectoryEntry));
            return serializer.Deserialize(stream) as DirectoryEntry;
        }

        public List<string> GetFullPaths(string prefix)
        {
            List<string> result = Files.Select(f => $"{prefix}/{f.Name}").ToList();
            foreach(DirectoryEntry dir in Directories)
                result.AddRange(dir.GetFullPaths($"{prefix}/{dir.Name}"));
            return result;
        }
    }
}
