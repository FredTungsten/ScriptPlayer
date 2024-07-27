using System;
using System.Xml.Serialization;

namespace ScriptPlayer.ViewModels
{
    [XmlType("Bookmark")]
    public class Bookmark
    {
        [XmlElement("Label")]
        public string Label { get; set; }

        [XmlElement("FilePath")]
        public string FilePath { get; set; }

        [XmlElement("Timestamp")]
        public long TimestampWrapper
        {
            get => Timestamp.Ticks;
            set => Timestamp = TimeSpan.FromTicks(value);
        }

        [XmlIgnore]
        public TimeSpan Timestamp { get; set; }
    }
}
