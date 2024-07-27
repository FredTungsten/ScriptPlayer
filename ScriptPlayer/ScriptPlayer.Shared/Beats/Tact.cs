using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptPlayer.Shared.Beats
{
    public class Tact
    {
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }

        public TimeSpan Duration
        {
            get => End - Start;
        }

        public TimeSpan BeatDuration
        {
            get => CalculateBeatDuration();
        }

        public int Beats { get; set; }

        public int BeatsPerBar { get; set; }

        private TimeSpan CalculateBeatDuration()
        {
            return Duration.Divide(Beats - 1);
        }

        public IEnumerable<TimeSpan> GetBeats(TimeSpan from, TimeSpan to)
        {
            TimeSpan duration = CalculateBeatDuration();
            return GetBeatIndices(from, to).Select(i => Start + duration.Multiply(i));
        }

        public IEnumerable<int> GetBeatIndices(TimeSpan from, TimeSpan to)
        {
            TimeSpan duration = CalculateBeatDuration();

            int firstBeat = (int)Math.Floor((from - Start).Divide(duration));
            int lastBeat = (int)Math.Ceiling((to - Start).Divide(duration));

            firstBeat = Math.Min(Beats - 1, Math.Max(0, firstBeat));
            lastBeat = Math.Min(Beats - 1, Math.Max(0, lastBeat));

            return Enumerable.Range(firstBeat, lastBeat - firstBeat + 1);
        }
    }
}