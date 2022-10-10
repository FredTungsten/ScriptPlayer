using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ScriptPlayer.Shared.Subtitles
{
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

                    ParseEntry(entry);

                    entries.Add(entry);
                }
            }
            catch
            {
                
            }

            return entries;
        }

        private static List<SubtitleFormat> _formats = new List<SubtitleFormat>
        {
            new SubtitleFormat("SubRip", "srt", "srt")
        };

        public override List<SubtitleFormat> GetSupportedFormats()
        {
            return _formats;
        }

        private Regex regex = new Regex("<(?<closing>/)?(?<tag>\\w+)((\\s+)(?<attribute>\\w+)=\\\"(?<value>[^\"]*)\\\")*>|{(?<closing>/)?(?<tag>\\w+)((\\s+)(?<attribute>\\w+)=\\\"(?<value>[^\"]*)\\\")*}", RegexOptions.Compiled);

        private void ParseEntry(SubtitleEntry entry)
        {
            StringBuilder builder = new StringBuilder();

            int textPosition = 0;
            int previousTagEnd = 0;

            List<SubtitleOption> activeOptions = new List<SubtitleOption>();

            foreach (Match m in regex.Matches(entry.Markup))
            {
                Capture capture = m.Captures[0];

                if (capture.Index != previousTagEnd)
                {
                    int length = capture.Index - previousTagEnd;
                    builder.Append(entry.Markup, previousTagEnd, length);
                    textPosition += length;
                }

                previousTagEnd = capture.Index + capture.Length;

                //Closing Tag?
                if (m.Groups["closing"].Length > 0)
                {
                    for (int i = 0; i < activeOptions.Count; i++)
                    {
                        if (activeOptions[i].Option == m.Groups["tag"].Value)
                        {
                            activeOptions[i].PositionTo = textPosition;
                            activeOptions.RemoveAt(i);
                            break;
                        }
                    }
                }
                else
                {
                    SubtitleOption option = new SubtitleOption();
                    option.Option = m.Groups["tag"].Value;
                    option.PositionFrom = textPosition;

                    entry.Options.Add(option);
                    activeOptions.Insert(0, option);
                }
            }

            //If the markup is broken or parsed incorrectly - close Options
            if (activeOptions.Count > 0)
            {
                foreach (var option in activeOptions)
                    option.PositionTo = textPosition;
            }

            if (previousTagEnd != entry.Markup.Length)
                builder.Append(entry.Markup, previousTagEnd, entry.Markup.Length - previousTagEnd);

            entry.Text = builder.ToString();
        }
    }
}