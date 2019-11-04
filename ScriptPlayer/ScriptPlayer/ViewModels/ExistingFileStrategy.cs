using System.Xml.Serialization;

namespace ScriptPlayer.ViewModels
{
    public enum ExistingFileStrategy
    {
        [XmlEnum("Skip")]
        Skip = 1,

        [XmlEnum("Replace")]
        Replace = 2,

        [XmlEnum("RenameOld")]
        RenameOld = 3,

        [XmlEnum("RenameNew")]
        RenameNew = 4
    }
}