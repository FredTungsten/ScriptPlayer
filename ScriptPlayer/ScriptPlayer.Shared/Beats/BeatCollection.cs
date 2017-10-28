using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ScriptPlayer.Shared
{
    public class BeatCollection : ICollection<TimeSpan>
    {
        private readonly List<TimeSpan> _beats;

        private static CultureInfo _culture = new CultureInfo("en-us");
        public BeatCollection()
        {           
            _beats =  new List<TimeSpan>();
        }

        public BeatCollection(IEnumerable<TimeSpan> beats)
        {
            _beats = new List<TimeSpan>(beats);
            _beats.Sort();
        }

        public TimeSpan this[int index]
        {
            get => _beats[index];
        }

        public bool Remove(TimeSpan item)
        {
            return _beats.Remove(item);
        }

        public int Count => _beats.Count;
        public bool IsReadOnly => false;

        public IEnumerable<TimeSpan> GetBeats(TimeSpan timestampFrom, TimeSpan timestampTo)
        {
            return _beats.Where(b => b >= timestampFrom && b <= timestampTo);
        }

        public void Add(TimeSpan beat)
        {
            _beats.Add(beat);
        }

        public void Clear()
        {
            _beats.Clear();
        }

        public bool Contains(TimeSpan item)
        {
            return _beats.Contains(item);
        }

        public void CopyTo(TimeSpan[] array, int arrayIndex)
        {
            _beats.CopyTo(array, arrayIndex);
        }

        public IEnumerator<TimeSpan> GetEnumerator()
        {
            return _beats.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public BeatCollection Scale(double scale)
        {
            return new BeatCollection(_beats.Select(b => b.Multiply(scale)));
        }

        public BeatCollection Shift(TimeSpan shift)
        {
            return new BeatCollection(_beats.Select(b => b.Add(shift)));
        }

        public BeatCollection Duplicate()
        {
            return new BeatCollection(_beats);
        }

        public void Save(string filename)
        {
            using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                using (TextWriter writer = new StreamWriter(stream))
                {
                    foreach (TimeSpan beat in _beats)
                        writer.WriteLine(beat.TotalSeconds.ToString("f3", _culture));
                }
            }
        }

        public static BeatCollection Load(string filename)
        {
            using(FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                return Load(stream);
        }

        public static BeatCollection Load(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                List<TimeSpan> beats = new List<TimeSpan>();
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    beats.Add(TimeSpan.FromSeconds(double.Parse(line.Replace(",", "."), _culture)));
                }

                return new BeatCollection(beats);
            }
        }

        public void Sort()
        {
            _beats.Sort();
        }

        public int IndexOf(TimeSpan timestamp)
        {
            return _beats.IndexOf(timestamp);
        }
    }
}
