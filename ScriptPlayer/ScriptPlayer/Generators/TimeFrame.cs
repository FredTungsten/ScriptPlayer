using System;
using ScriptPlayer.Shared;

namespace ScriptPlayer.Generators
{
    public class TimeFrame
    {
        public double StartFactor { get; set; }
        public TimeSpan StartTimeSpan { get; set; }
        public TimeSpan Duration { get; set; }

        public TimeFrame()
        {
            StartTimeSpan = TimeSpan.MinValue;
            StartFactor = double.NaN;

            Duration = TimeSpan.FromSeconds(5);
        }

        public bool IsFactor => !double.IsNaN(StartFactor);

        public void CalculateStart(TimeSpan duration)
        {
            if (double.IsNaN(StartFactor))
                return;

            StartTimeSpan = duration.Multiply(StartFactor);
        }

        public TimeFrame Duplicate()
        {
            return new TimeFrame
            {
                Duration = Duration,
                StartFactor = StartFactor,
                StartTimeSpan = StartTimeSpan
            };
        }
    }
}