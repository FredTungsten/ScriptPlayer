using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScriptPlayer.Shared.Scripts
{
    public class ScriptFileFormatCollection
    { 
        private readonly List<ScriptFileFormat> _list;
        private bool _includeAll;

        public ScriptFileFormat[] GetFormats(int selectedIndex, string filename)
        {
            string extension = Path.GetExtension(filename)?.TrimStart('.').ToLower();

            if ((_includeAll && selectedIndex == 0) || (selectedIndex < 0))
                return GetFormatsByExtension(extension);

            if(_includeAll)
                selectedIndex--;

            if(selectedIndex >= _list.Count)
                throw new ArgumentException("A ScriptFileFormat with the specified selectedIndex does not exist!", nameof(selectedIndex));

            return new[]{_list[selectedIndex]};
        }

        private ScriptFileFormat[] GetFormatsByExtension(string extension)
        {
            return _list.Where(f => f.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase)).ToArray();
        }

        public string BuildFilter(bool includeAll)
        {
            _includeAll = includeAll;

            _list.Sort((a,b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            List<string> filterEntries = new List<string>();

            if(includeAll)
                filterEntries.Add(BuildFilter("All Script Formats", _list.SelectMany(f => f.Extensions).Distinct().OrderBy(e => e)));

            foreach (ScriptFileFormat format in _list)
            {
                filterEntries.Add(BuildFilter(format.Name, format.Extensions));
            }

            return string.Join("|", filterEntries);
        }

        private static string BuildFilter(string name, IEnumerable<string> extensions)
        {
            return name + "|" + string.Join(";", extensions.Select(e => "*." + e));
        }

        public ScriptFileFormatCollection(IEnumerable<ScriptFileFormat> initialData)
        {
            _list = new List<ScriptFileFormat>(initialData);
        }
    }
}