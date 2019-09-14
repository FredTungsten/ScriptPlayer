using System.Xml.Serialization;

namespace ScriptPlayer.ViewModels
{
    public enum ChapterMode
    {
        [XmlEnum("RandomChapter")]
        RandomChapter,

        [XmlEnum("FastestChapter")]
        FastestChapter,

        [XmlEnum("RandomTimeSpan")]
        RandomTimeSpan,

        [XmlEnum("FastestTimeSpan")]
        FastestTimeSpan,

        [XmlEnum("RandomChapterLimitedDuration")]
        RandomChapterLimitedDuration,

        [XmlEnum("FastestChapterLimitedDuration")]
        FastestChapterLimitedDuration
    }
}