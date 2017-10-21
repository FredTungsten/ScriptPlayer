using System;

namespace ScriptPlayer.Shared
{
    public static class SpeedPredictor
    {
        public static double FullLengthsPerSecond = 6.0;
        public static double TurnaroundDelay = 0.02; // 0.05;

        // Intervall ~= (Range / 99) * (Speed / 99) * 1/6s
        // (Speed * 99) ~= Intervall / (Range / 99.0 * 1/6s)
        public static byte PredictSpeed(byte range, TimeSpan duration)
        {
            double relativeLength = range / 99.0;
            double durationAtFullSpeed = TurnaroundDelay + relativeLength / FullLengthsPerSecond;
            double requiredSpeed = durationAtFullSpeed / duration.TotalSeconds;
            byte actualSpeed = ClampValue(requiredSpeed * 99.0);
            return actualSpeed;
        }

        public static byte PredictDistance(byte speed, TimeSpan duration)
        {
            double usableTime = duration.TotalSeconds - TurnaroundDelay;
            double distance = usableTime * (FullLengthsPerSecond * (speed / 99.0));
            byte absoluteDistance = ClampValue(distance * 99.0);
            return absoluteDistance;
        }

        public static byte ClampValue(double value)
        {
            return (byte)Math.Min(99, Math.Max(0, Math.Round(value, MidpointRounding.AwayFromZero)));
        }

        //by funjack:
        //func Speed(dist int, dur time.Duration) (speed int) { 
        //mil := dur.Nanoseconds() / 1e6 * int64(90/dist)
        //speed = int (25000 * math.Pow(float64(mil), -1.05))
        //return speed

        public static byte PredictSpeed2(byte range, TimeSpan duration)
        {
            double mil = duration.TotalMilliseconds * 90 / range;
            double speed = 25000 * Math.Pow(mil, -1.05);
            return ClampValue(speed);
        }

        public static byte PredictSpeed2(byte pFrom, byte pTo, TimeSpan duration)
        {
            return PredictSpeed2((byte) Math.Abs(pFrom - pTo), duration);
        }
    }
}
