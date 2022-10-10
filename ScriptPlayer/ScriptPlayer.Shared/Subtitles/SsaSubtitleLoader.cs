using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ScriptPlayer.Shared.Subtitles
{
    public class SsaSubtitleLoader : SubtitleLoader
    {
        public SsaSubtitleLoader()
        { }

        public override List<SubtitleEntry> LoadEntriesFromLines(string[] lines)
        {
            List<SubtitleEntry> entries = new List<SubtitleEntry>();

            IndexedProperties propertyMap = null;
            string section = "";

            foreach (string originalLine in lines)
            {
                string line = originalLine.Trim();

                if (string.IsNullOrEmpty(line))
                    continue;

                //comment
                if(line.StartsWith(";"))
                    continue;

                //section definition
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();
                    propertyMap = null;
                    continue;
                }

                int pos = line.IndexOf(':');
                if(pos <= 0)
                    continue;

                string command = line.Substring(0, pos).Trim().ToLowerInvariant();
                string value = line.Substring(pos + 1);

                switch (section)
                {
                    case "events":
                    {
                        switch (command)
                        {
                            case "format":
                            {
                                propertyMap = new IndexedProperties(value);
                                break;
                            }
                            case "dialogue":
                            {
                                if (propertyMap == null)
                                    continue;

                                PropertyCollection properites = new PropertyCollection(propertyMap, value);

                                SubtitleEntry entry = new SubtitleEntry
                                {
                                    From = ParseTimeStamp(properites["start"]),
                                    To = ParseTimeStamp(properites["end"]),
                                    Markup = properites["text"],
                                };
                                ParseEntry(entry);
                                entries.Add(entry);
                                break;
                            }
                        }

                        break;
                    }
                }
            }

            return entries;
        }

        private Regex regex = new Regex("{(?<tag>.*?)}", RegexOptions.Compiled);

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

                SubtitleOption option = new SubtitleOption();
                option.Option = m.Groups["tag"].Value;
                option.PositionFrom = textPosition;

                entry.Options.Add(option);
                activeOptions.Insert(0, option);                
            }

            //If the markup is broken or parsed incorrectly - close Options
            if (activeOptions.Count > 0)
            {
                foreach (var option in activeOptions)
                    option.PositionTo = textPosition;
            }

            if (previousTagEnd != entry.Markup.Length)
                builder.Append(entry.Markup, previousTagEnd, entry.Markup.Length - previousTagEnd);

            entry.Text = builder.ToString().Replace("\\N", "\n").Replace("\\n", "\n");
        }

        private TimeSpan ParseTimeStamp(string value)
        {
            const string timestampFormat = "h\\:mm\\:ss\\.ff";
            if(TimeSpan.TryParseExact(value, timestampFormat, CultureInfo.InvariantCulture, out TimeSpan timestamp))
                return timestamp;

            if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out timestamp))
                return timestamp;

            return TimeSpan.MaxValue;
        }

        private static List<SubtitleFormat> _formats = new List<SubtitleFormat>
        {
            new SubtitleFormat("Sub Station Alpha", "ssa", "ssa"),
            new SubtitleFormat("Advanced Sub Station Alpha", "ass", "ass"),
        };

        public override List<SubtitleFormat> GetSupportedFormats()
        {
            return _formats;
        }
    }

    public class IndexedProperties
    {
        private readonly Dictionary<string,int> _positions = new Dictionary<string, int>();

        public IndexedProperties(string format)
        {
            string[] properties = format.Split(',').Select(r => r.Trim().ToLowerInvariant()).ToArray();
            for(int i = 0; i < properties.Length; i++)
                _positions.Add(properties[i], i);
        }

        public int Count => _positions.Count;

        public int GetIndex(string property)
        {
            if (_positions.ContainsKey(property.ToLowerInvariant()))
                return _positions[property.ToLowerInvariant()];
            
            return -1;
        }
    }

    public class PropertyCollection
    {
        private IndexedProperties _properties;
        private string[] _values;

        public PropertyCollection(IndexedProperties properties, string csv)
        {
            _properties = properties;
            
            List<string> values = new List<string>();

            int previousValueEnd = 0;

            for (int i = 0; i < properties.Count; i++)
            {
                if (previousValueEnd < 0)
                {
                    values.Add(null);
                    continue;
                }
                
                if (i + 1 >= properties.Count)
                {
                    if(previousValueEnd <= csv.Length)
                        values.Add(csv.Substring(previousValueEnd));
                    else
                        values.Add(null);
                }
                else
                {
                    
                        int commaPos = csv.IndexOf(',', previousValueEnd);
                        if (commaPos >= 0)
                        {
                            values.Add(csv.Substring(previousValueEnd, commaPos - previousValueEnd));
                            previousValueEnd = commaPos + 1;
                        }
                        else
                        {
                            values.Add(null);
                        }
                }
            }

            _values = values.ToArray();
        }

        public string this[string property]
        {
            get
            {
                int index = _properties.GetIndex(property);
                if (index >= 0 && index < _values.Length)
                    return _values[index];

                return null;
            }
        }
    }
}