﻿using System;

namespace ScriptPlayer.Shared
{
    public static class CommandConverter
    {
        /*public static uint LaunchToVorzeSpeed(DeviceCommandInformation info)
        {
            //Information from https://github.com/metafetish/syncydink/blob/4c8c31d6f8ffba2c9d1f3fcb69209630b209cd89/src/utils/HapticsToButtplug.ts#L186

            double delta = Math.Abs(info.PositionFromOriginal - (double)info.PositionToOriginal) / 99.0;
            double speed = Math.Floor(25000 * Math.Pow(info.Duration.TotalMilliseconds / delta, -1.05)) / 100.0;
            // 100ms = ~0.95
            speed = info.TransformSpeed(speed) * 100.0;

            return (uint)speed;
        }*/

        public static uint SimpleVorzeSpeed(DeviceCommandInformation info)
        {
            double speed = Math.Abs(info.PositionFromOriginal - 50) / 50.0;
            speed = info.TransformSpeed(speed) * 99.0;

            return (uint)speed;
        }

        // Reverted to 0.0 by request of github user "sextoydb":
        // https://github.com/FredTungsten/ScriptPlayer/issues/64
        public static double LaunchToVibrator(byte position)
        {
            const double max = 1.0;
            const double min = 0.0;

            double speedRelative = 1.0 - ((position + 1) / 100.0);
            double result = min + (max - min) * speedRelative;
            return Math.Min(max, Math.Max(min, result));
        }

        public static uint LaunchToKiiroo(byte position, uint min, uint max)
        {
            double pos = position / 0.99;

            uint result = Math.Min(max, Math.Max(min, (uint)Math.Round(pos * (max - min) + min)));

            return result;
        }
    }
}
