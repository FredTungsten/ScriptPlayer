using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScriptPlayer.Shared.Subtitles
{
    

    public abstract class SubtitleLoader
    {
        public List<SubtitleEntry> LoadEntriesFromFile(string filename)
        {
            return LoadEntriesFromLines(File.ReadAllLines(filename));
        }

        public abstract List<SubtitleEntry> LoadEntriesFromLines(string[] lines);
        public abstract List<SubtitleFormat> GetSupportedFormats();
    }

    public class SubtitleOption
    {
        public string Option { get; set; }
        public string Value { get; set; }
        public int PositionFrom { get; set; }
        public int PositionTo { get; set; }
    }

    public class SubtitleHandler
    {
        private List<SubtitleEntry> _entries;
        private int _currentIndex;

        private HashSet<int> _activeEntries { get; set; }

        public void SetSubtitles(IEnumerable<SubtitleEntry> entries)
        {
            _entries = entries.ToList();
            _currentIndex = 0;
        }

        public List<SubtitleEntry> GetActiveEntries(TimeSpan timestamp)
        {
            //I know I should probably make an index for this, but it kind of doesn't feel necessary yet ...

            if(_entries == null)
                return new List<SubtitleEntry>();

            return _entries.Where(e => e.IsVisible(timestamp)).ToList();
        }
    }

    public class IndexedTimestamp
    {
        public TimeSpan Timestamp { get; set; }
        public int EntryIndex { get; set; }
    }

    public class SubtitleEntry
    {
        public TimeSpan From { get; set; }
        public TimeSpan To { get; set; }
        public string Markup { get; set; }
        public string Text { get; set; }
        public List<SubtitleOption> Options { get; set; }

        public SubtitleEntry()
        {
            Options = new List<SubtitleOption>();
        }

        public bool IsVisible(TimeSpan timestamp)
        {
            return timestamp >= From && timestamp <= To;
        }
    }
}
