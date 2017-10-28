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
    }

    public class IntermediateCommandInformation
    {
        public DeviceCommandInformation DeviceInformation;
        public double Progress;
    }
}
