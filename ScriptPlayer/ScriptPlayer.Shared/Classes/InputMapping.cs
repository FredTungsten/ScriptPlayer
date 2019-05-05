using System.Xml.Serialization;

namespace ScriptPlayer.Shared
{
    public class InputMapping
    {
        [XmlAttribute("Command")]
        public string CommandId { get; set; }

        [XmlAttribute("Shortcut")]
        public string KeyboardShortcut { get; set; }
    }
}