using System;

namespace ScriptPlayer.Shared
{
    public class QueueEntry
    {
        public QueueEntry(byte position, byte speed)
        {
            Position = position;
            Speed = speed;
            Submitted = DateTime.Now;
        }

        public DateTime Submitted { get; set; }
        public TimeSpan Due { get; set; }

        public byte Speed { get; set; }

        public byte Position { get; set; }
    }
}