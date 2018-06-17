using System;
using System.IO;
using System.Linq;

namespace ScriptPlayer.Shared.Helpers
{
    public static class FileFinder
    {
        public static string FindFile(string filename, string[] extensions, string[] additionalPaths)
        {
            //With removed second extension
            string stripped = TrimExtension(filename, extensions);
            if (File.Exists(stripped))
                return stripped;

            //Same directory, appended Extension
            foreach (string extension in extensions)
            {
                string path = AppendExtension(filename, extension);
                if (File.Exists(path))
                    return path;
            }

            //Same directory, exchanged extension
            foreach (string extension in extensions)
            {
                string path = Path.ChangeExtension(filename, extension);
                if (File.Exists(path))
                    return path;
            }

            if (additionalPaths == null)
                return null;

            //Addtional Directories, stripped second extension
            string fileNameWithoutSecondExtension = TrimExtension(Path.GetFileName(filename), extensions);
            if (!String.IsNullOrWhiteSpace(fileNameWithoutSecondExtension))
            {
                foreach (string path in additionalPaths)
                {
                    if (!Directory.Exists(path)) continue;

                    string newPath = Path.Combine(path, fileNameWithoutSecondExtension);
                    if (File.Exists(newPath))
                        return newPath;
                }
            }

            //Additional Directories, appended extension
            string fileNameWithExtension = Path.GetFileName(filename);
            if (string.IsNullOrWhiteSpace(fileNameWithExtension))
                return null;

            foreach (string path in additionalPaths)
            {
                if (!Directory.Exists(path)) continue;

                string basePath = Path.Combine(path, fileNameWithExtension);

                foreach (string extension in extensions)
                {
                    string expectedPath = AppendExtension(basePath, extension);
                    if (File.Exists(expectedPath))
                        return expectedPath;
                }
            }

            //Additional Directories, exchanged extension
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
                return null;

            foreach (string path in additionalPaths)
            {
                if (!Directory.Exists(path)) continue;

                string basePath = Path.Combine(path, fileNameWithoutExtension);

                foreach (string extension in extensions)
                {
                    string expectedPath = AppendExtension(basePath, extension);
                    if (File.Exists(expectedPath))
                        return expectedPath;
                }
            }

            return null;
        }

        private static string TrimExtension(string filename, string[] extensions)
        {
            string newfilename = GetTrimmed(Path.GetFileName(filename), extensions);
            if (String.IsNullOrWhiteSpace(newfilename)) return null;

            return Path.Combine(Path.GetDirectoryName(filename), newfilename);
        }

        private static string GetTrimmed(string filename, string[] extensions)
        {
            if (filename.Count(c => c == '.') < 2)
                return null;

            int lastDot = filename.LastIndexOf(".", StringComparison.InvariantCulture);
            string trimmedFileName = filename.Substring(0, lastDot);
            string firstExtension = Path.GetExtension(trimmedFileName).TrimStart('.');
            return extensions.Any(e => string.Equals(e, firstExtension, StringComparison.OrdinalIgnoreCase)) ? trimmedFileName : null;
        }

        private static string AppendExtension(string filename, string extension)
        {
            string result = filename;
            if (!extension.StartsWith("."))
                result += ".";
            result += extension;
            return result;
        }
    }
}
