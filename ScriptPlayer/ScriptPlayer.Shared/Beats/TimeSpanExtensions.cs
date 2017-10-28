using System;

namespace ScriptPlayer.Shared
{
    public static class TimeSpanExtensions
    {
        public static double Divide(this TimeSpan t1, TimeSpan t2)
        {
            return t1.Ticks / (double)t2.Ticks;
        }

        public static TimeSpan Divide(this TimeSpan t1, double value)
        {
            return TimeSpan.FromTicks((long)(t1.Ticks / value));
        }

        public static TimeSpan Multiply(this TimeSpan t1, double value)
        {
            return TimeSpan.FromTicks((long)(t1.Ticks * value));
        }

        public static TimeSpan Abs(this TimeSpan t)
        {
            return t.Ticks < 0 ? -t : t;
        }
    }
}
