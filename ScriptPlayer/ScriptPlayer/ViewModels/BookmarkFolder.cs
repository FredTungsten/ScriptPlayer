using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ScriptPlayer.ViewModels
{
    [XmlRoot("Bookmarks")]
    [XmlType("Folder")]
    public class BookmarkFolder
    {
        [XmlElement("Label")]
        public string Label { get; set; }

        [XmlArray("Bookmarks")]
        [XmlArrayItem("Bookmark")]
        public List<Bookmark> Bookmarks { get; set; }

        [XmlArray("Folders")]
        [XmlArrayItem("Folder")]
        public List<BookmarkFolder> Folders { get; set; }

        public void Save(string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
                Save(stream);
        }

        public void Save(Stream stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(BookmarkFolder));
            serializer.Serialize(stream, this);
        }

        public static BookmarkFolder FromFile(string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
                return FromStream(stream);
        }

        public static BookmarkFolder FromStream(Stream stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(BookmarkFolder));
            return serializer.Deserialize(stream) as BookmarkFolder;
        }
    }
}