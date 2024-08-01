using System;

namespace ScriptPlayer.Shared.Beats
{
    public struct TimeFrame
    {
        public static readonly TimeFrame Nothing = new TimeFrame(TimeSpan.MaxValue, TimeSpan.MinValue);
        public static readonly TimeFrame Everything = new TimeFrame(TimeSpan.MinValue, TimeSpan.MaxValue);

        public TimeSpan From { get; set; }
        public TimeSpan To { get; set; }

        public TimeFrame(TimeSpan from, TimeSpan to)
        {
            if (to > from)
            {
                From = from;
                To = to;
            }
            else
            {
                From = to;
                To = from;
            }
        }

        public bool Contains(TimeSpan timespan)
        {
            return timespan >= From && timespan <= To;
        }

        public bool Intersects(TimeFrame timeframe)
        {
            return timeframe.From <= To && timeframe.To >= From;
        }

        public TimeFrame Shift(TimeSpan timespan)
        {
            return new TimeFrame(From.Add(timespan), To.Add(timespan));
        }

        public TimeFrame Intersection(TimeFrame timeFrame)
        {
            if (Equals(Nothing) || timeFrame.Equals(Nothing))
                return Nothing;

            TimeSpan from = timeFrame.From > From ? timeFrame.From : From;
            TimeSpan to = timeFrame.To < To ? timeFrame.To : To;

            if (from < to)
                return new TimeFrame(from, to);
            else
                return Nothing;
        }
    }
}
