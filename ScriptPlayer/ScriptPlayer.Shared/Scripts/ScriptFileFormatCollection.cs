using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScriptPlayer.Shared.Scripts
{
    public class ScriptFileFormatCollection
    { 
        private List<ScriptFileFormat> _list;
        private bool _includeAll;
        private bool _includeAny;

        public ScriptFileFormat GetFormat(int selectedIndex, string filename)
        {
            string extension = Path.GetExtension(filename).TrimStart('.').ToLower();

            if ((_includeAll && selectedIndex == 0) || (selectedIndex < 0))
                return GetFormatByExtension(extension);

            if(_includeAll)
                selectedIndex--;

            if(selectedIndex >= _list.Count)
                throw new ArgumentException("A ScriptFileFormat with the specified selectedIndex does not exist!", nameof(selectedIndex));

            return _list[selectedIndex];
        }

        private ScriptFileFormat GetFormatByExtension(string extension)
        {
            return _list.FirstOrDefault(f => f.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase));
        }

        public string BuildFilter(bool includeAll)
        {
            _includeAll = includeAll;

            _list.Sort((a,b) => String.Compare(a.Name, b.Name, StringComparison.Ordinal));

            List<string> filterEntries = new List<string>();

            if(includeAll)
                filterEntries.Add(BuildFilter("All Script Formats", _list.SelectMany(f => f.Extensions)));

            foreach (ScriptFileFormat format in _list)
            {
                filterEntries.Add(BuildFilter(format.Name, format.Extensions));
            }

            return String.Join("|", filterEntries);
        }

        private string BuildFilter(string name, IEnumerable<string> extensions)
        {
            return name + "|" + String.Join(";", extensions.Select(e => "*." + e));
        }

        public ScriptFileFormatCollection(IEnumerable<ScriptFileFormat> initialData)
        {
            _list = new List<ScriptFileFormat>(initialData);
        }
    }
}