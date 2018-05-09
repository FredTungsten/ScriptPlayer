using System;

namespace ScriptPlayer.Shared
{
    public class DeviceCommandInformation
    {
        public byte PositionFromTransformed;
        public byte PositionToTransformed;
        public byte SpeedTransformed;

        public byte PositionFromOriginal;
        public byte PositionToOriginal;
        public byte SpeedOriginal;

        public TimeSpan Duration;
        public double SpeedMultiplier { get; set; } = 1;
        public double SpeedMin { get; set; } = 0;
        public double SpeedMax { get; set; } = 1;
        public double PlaybackRate { get; set; } = 1;
        public TimeSpan DurationStretched { get; set; }

        public double TransformSpeed(double speed)
        {
            return Math.Min(SpeedMax, Math.Max(SpeedMin, speed * SpeedMultiplier));
        }
    }

    public class IntermediateCommandInformation
    {
        public DeviceCommandInformation DeviceInformation;
        public double Progress;
    }
}
