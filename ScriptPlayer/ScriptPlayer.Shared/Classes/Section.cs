using System;
using System.Diagnostics;

namespace ScriptPlayer.Shared.Classes
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class Section
    {

        private string DebuggerDisplay => $"{Start:g} - {End:g} ({Duration:g})";

        public bool IsEmpty => Start == End;

        public TimeSpan Start { get; protected set; }
        public TimeSpan End { get; protected set; }

        public TimeSpan Duration { get; protected set; }
        public static Section Empty => new Section(TimeSpan.Zero, TimeSpan.Zero);

        public static Section All => new Section(TimeSpan.MinValue, TimeSpan.MaxValue);

        protected Section()
        {
        }

        public Section(TimeSpan start, TimeSpan end)
        {
            Start = start > end ? end : start;
            End = end > start ? end : start;

            Duration = End - Start;
        }

        public bool Contains(TimeSpan timeStamp, bool includeBorders)
        {
            if (includeBorders)
                return timeStamp >= Start && timeStamp <= End;

            return timeStamp > Start && timeStamp < End;
        }

        public bool Overlaps(Section section, bool includeBorders)
        {
            if(includeBorders)
                return section.Start <= End && section.End >= Start;

            return section.Start < End && section.End > Start;
        }
    }
}
