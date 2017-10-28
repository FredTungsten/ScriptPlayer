using System.Linq;

namespace ScriptPlayer.Shared
{
    public class BeatPattern
    {
        public const double BeatEnd = 0.9999999999;

        public double Duration { get { return 4.0; } }

        public static BeatPattern Pause = new BeatPattern();
        public static BeatPattern VerySlow = FromEights(0);
        public static BeatPattern Slow = FromEights(0, 4);
        public static BeatPattern Normal = FromEights(0, 2, 4, 6);
        public static BeatPattern Double = FromEights(0, 1, 2, 3, 4, 5, 6, 7);

        public static BeatPattern Pattern33 = FromEights(0, 1, 2, 4, 5, 6);
        public static BeatPattern Pattern32 = FromEights(0, 1, 2, 4, 6);
        public static BeatPattern Pattern51 = FromEights(0, 1, 2, 3, 4, 6);
        public static BeatPattern Pattern7 = FromEights(0, 1, 2, 3, 4, 5, 6);

        private static BeatPattern FromEights(params int[] eights)
        {
            double[] position = eights.Select(e => e / 8.0).ToArray();
            return new BeatPattern(position);
        }

        public double[] BeatPositions { get; set; }

        public BeatPattern(params double[] positions)
        {
            BeatPositions = positions;
        }

        public double GetAbsolutePosition(double relativeBeatPosition)
        {
            if (BeatPositions.Length == 0) return BeatEnd;
            if (BeatPositions.Length == 1)
            {
                double val = relativeBeatPosition - BeatPositions[0];
                if (val < 0) return val + 1;
                if (val > 1) return val - 1;
                return val;
            }

            for (int i = 0; i < BeatPositions.Length; i++)
            {
                if (BeatPositions[i] >= relativeBeatPosition)
                {
                    if (i == 0)
                    {
                        return CalulateProgress(BeatPositions[BeatPositions.Length - 1], BeatPositions[0] + 1, relativeBeatPosition);
                    }
                    return CalulateProgress(BeatPositions[i - 1], BeatPositions[i], relativeBeatPosition);
                }
            }
            return CalulateProgress(BeatPositions[BeatPositions.Length - 1], BeatPositions[0] + 1, relativeBeatPosition);
        }

        private double CalulateProgress(double min, double max, double value)
        {
            return (value - min) / (max - min);
        }
    }
}