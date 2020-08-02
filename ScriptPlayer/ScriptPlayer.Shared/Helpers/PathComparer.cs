using System;
using System.Collections.Generic;
using System.IO;

namespace ScriptPlayer.Shared.Helpers
{
    public class PathComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return PathEquals(x, y);
        }

        public static bool PathEquals(string x, string y)
        {
            return string.Equals(Normalize(x), Normalize(y), StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(string value)
        {
            return Normalize(value).GetHashCode();
        }

        private static string Normalize(string value)
        {
            return Path.GetFullPath(value);
        }
    }
}
