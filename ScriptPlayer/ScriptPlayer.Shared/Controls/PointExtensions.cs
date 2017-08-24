using System;
using System.Windows;

namespace ScriptPlayer.Shared
{
    public static class PointExtensions
    {
        public static double DistanceTo(this Point point, Point other)
        {
            return Math.Sqrt(Math.Pow(point.X - other.X, 2) + Math.Pow(point.Y - other.Y, 2));
        }
    }
}