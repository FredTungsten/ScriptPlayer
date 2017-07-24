using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ScriptPlayer.Shared.Scripts
{
    public class IniFile
    {
        public List<IniCategory> Categories { get; set; }

        public IniFile()
        {
            Categories = new List<IniCategory>();
        }

        public static IniFile FromFile(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                return FromStream(stream);
        }

        public static IniFile FromStream(Stream stream)
        {
            IniFile result = new IniFile();
            IniCategory currentCategory = null;

            using (StreamReader reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    line = line.Trim();

                    if (line.StartsWith("["))
                    {
                        int to = line.IndexOf("]", StringComparison.Ordinal);
                        string name = line.Substring(1, to - 1);
                        currentCategory = new IniCategory { Name = name };
                        result.Categories.Add(currentCategory);
                    }
                    else if (currentCategory == null)
                    {
                        Debug.WriteLine("Uncategorized Entry: " + line);
                    }
                    else
                    {
                        int equalSign = line.IndexOf("=", StringComparison.Ordinal);
                        if (equalSign < 0)
                        {
                            Debug.WriteLine("Can't parse line: " + line);
                        }
                        else
                        {
                            string name = line.Substring(0, equalSign);
                            string value = line.Substring(equalSign + 1);
                            currentCategory.Entries.Add(new IniEntry
                            {
                                Name = name,
                                Value = value
                            });
                        }
                    }
                }
            }

            return result;
        }

        public IniCategory this[string name]
        {
            get
            {
                return Categories.FirstOrDefault(
                    e => String.Equals(e.Name, name, StringComparison.InvariantCultureIgnoreCase));
            }
        }
    }

    public class IniCategory
    {
        public IniEntry this[string name]
        {
            get
            {
                return Entries.FirstOrDefault(
                    e => String.Equals(e.Name, name, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public string Name { get; set; }
        public List<IniEntry> Entries { get; set; }

        public IniCategory()
        {
            Entries = new List<IniEntry>();
        }
    }

    public class IniEntry
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
