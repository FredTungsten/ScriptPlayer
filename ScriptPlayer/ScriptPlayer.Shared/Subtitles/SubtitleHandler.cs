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
    }

    public class SrtSubtitleLoader : SubtitleLoader
    {
        public override List<SubtitleEntry> LoadEntriesFromLines(string[] lines)
        {
            List<SubtitleEntry> entries = new List<SubtitleEntry>();

            try
            {
                Queue<string> l = new Queue<string>(lines);

                while(l.Count > 0)
                {
                    int lineNo = int.Parse(l.Dequeue());
                    string timestamps = l.Dequeue();
                    int arrowPos = timestamps.IndexOf("-->", StringComparison.Ordinal);

                    string sFrom = timestamps.Substring(0, arrowPos).Trim();
                    string sTo = timestamps.Substring(arrowPos + 3).Trim();

                    TimeSpan tFrom = TimeSpan.Parse(sFrom);
                    TimeSpan tTo = TimeSpan.Parse(sTo);

                    string markup = "";
                    string nextLine = "";
                    do
                    {
                        if (l.Count == 0)
                            nextLine = "";
                        else
                            nextLine = l.Dequeue();

                        if(!string.IsNullOrWhiteSpace(nextLine))
                        {
                            if (!string.IsNullOrWhiteSpace(markup))
                                markup += "\r\n";

                            markup += nextLine;
                        }
                    } while (!string.IsNullOrWhiteSpace(nextLine));

                    SubtitleEntry entry = new SubtitleEntry
                    {
                        From = tFrom,
                        To = tTo,
                        Markup = markup
                    };

                    entries.Add(entry);
                }
            }
            catch
            {
                
            }

            return entries;
        }
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

        public bool IsVisible(TimeSpan timestamp)
        {
            return timestamp >= From && timestamp <= To;
        }
    }
}
