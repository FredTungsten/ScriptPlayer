using System;

namespace ScriptPlayer.Shared
{
    public class BeatGroup
    {
        public BeatPattern Pattern;
        public double BeatsPerMinute;
        public double Start { get; set; }
        public int Repetitions { get; set; }

        public BeatGroup()
        {
            
        }

        public static BeatGroup SingleBeat(TimeSpan duration)
        {
            BeatPattern pattern = BeatPattern.VerySlow;
            double beatsPerMinute = 60 * (pattern.Duration / duration.TotalSeconds);
            return new BeatGroup(pattern, beatsPerMinute, 1);
        }
        public static BeatGroup Pause(TimeSpan duration)
        {
            BeatPattern pattern = BeatPattern.Pause;
            double beatsPerMinute = 60 * (pattern.Duration / duration.TotalSeconds);
            return new BeatGroup(pattern, beatsPerMinute,1);
        }

        public BeatGroup(BeatPattern pattern, double bpm, int repetitions)
        {
            Pattern = pattern;
            BeatsPerMinute = bpm;
            Repetitions = repetitions;
        }

        public BeatGroup(BeatPattern pattern, double bpm, TimeSpan minimalDuration)
        {
            Pattern = pattern;
            BeatsPerMinute = bpm;

            double reps = minimalDuration.TotalSeconds/pattern.Duration*BeatsPerMinute/60.0;

            Repetitions = (int)Math.Ceiling(reps);
        }

        public double Duration
        {
            get { return Repetitions * ActualPatternDuration; }
        }

        public double ActualPatternDuration
        {
            get { return (60.0 / BeatsPerMinute) * Pattern.Duration; }
        }

        public double End
        {
            get { return Start + Duration; }
        }

        public double GetBeatProgress(double position)
        {
            double relativeProgress = position - Start;
            double finishedRepetitions = Math.Floor(relativeProgress / ActualPatternDuration);
            double beatProgress = relativeProgress - ActualPatternDuration * finishedRepetitions;
            double beatRelativeProgress = Pattern.GetAbsolutePosition(beatProgress / ActualPatternDuration);
            return beatRelativeProgress;
        }

        public double FindStartingPoint(double position)
        {
            double relativeStart = position - Start;
            double completedRepetitions = Math.Floor(relativeStart / ActualPatternDuration);
            double absoluteStart = Start + completedRepetitions * ActualPatternDuration;
            return absoluteStart;
        }
    }
}