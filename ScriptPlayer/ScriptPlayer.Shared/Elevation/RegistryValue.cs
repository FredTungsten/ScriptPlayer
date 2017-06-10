using Microsoft.Win32;

namespace UacElevation
{
    public class RegistryValue
    {
        public string Path { get; set; }

        public string Name { get; set; }
        public RegistryValueKind Type { get; set; }
        public object Value { get; set; }

        public bool Exists => Type != RegistryValueKind.None && Type != RegistryValueKind.Unknown;

        public RegistryValue()
        {
            Type = RegistryValueKind.None;
            Value = null;
        }

        public RegistryValue(string path, string name) : this()
        {
            Path = path;
            Name = name;
        }
    }
}