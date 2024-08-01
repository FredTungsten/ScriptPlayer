using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ScriptPlayer.Shared
{
    public class BeatProject
    {
        public string VideoFile { get; set; }
       
        public double BeatBarDuration { get; set; }
        public double BeatBarMidpoint { get; set; }
        public List<long> Beats { get; set; }
        public List<long> Bookmarks { get; set; }

        public List<TimedPosition> Positions { get; set; }
        public List<TactDefinition> Tacts { get; set; }


        public BeatProject()
        {

        }

        public void Save(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(BeatProject));
                serializer.Serialize(stream, this);
            }
        }

        public static BeatProject Load(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(BeatProject));
                return serializer.Deserialize(stream) as BeatProject;
            }
        }
    }

    public class BeatSegment
    {
        public long Position { get; set; }
        public long Duration { get; set; }

        public BeatDefinition Beat { get; set; }
        public bool TimeLocked { get; set; }
        public long PatternDuration { get; set; }
    }

    public class BeatDefinition
    {
        public bool[] Pattern;
    }

    public class TactDefinition
    {
        public int Beats { get; set; }
        public int BeatsPerBar { get; set; }
        public long Start { get; set; }
        public long End { get; set; }

        public List<BarDefinition> Bars { get; set; }
    }

    public class BarDefinition
    {
        public int Start { get; set; }
        public int Length { get; set; }
        public bool[] Rythm { get; set; }
        public int Subdivisions { get; set; }
    }
}
