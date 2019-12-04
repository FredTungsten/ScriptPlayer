using System;
using System.Xml.Serialization;

namespace ScriptPlayer.Shared
{
    public class TimedPosition
    {
        [XmlAttribute("TimeStamp")]
        public long TimeStampWrapper
        {
            get => TimeStamp.Ticks;
            set => TimeStamp = TimeSpan.FromTicks(value);
        }

        [XmlIgnore]
        public TimeSpan TimeStamp;

        [XmlAttribute("Position")]
        public byte Position;

        public TimedPosition Duplicate()
        {
            return new TimedPosition
            {
                TimeStamp = TimeStamp,
                Position = Position
            };
        }
    }
}