using System;
using System.Windows;
using System.Windows.Media;

namespace ScriptPlayer.Shared.Helpers
{
    public static class ResizeHelper
    {
        public static Rect CenterInRect(Size destSize, Rect rect)
        {
            double offsetX = (rect.Width - destSize.Width) / 2;
            double offsetY = (rect.Height - destSize.Height) / 2;

            return new Rect(new Point(offsetX, offsetY), destSize);
        }

        public static Size StretchSize(Stretch stretchMode, Size imageSize, Size destSize)
        {
            double ratio = imageSize.Width / imageSize.Height;

            double resultWidth = destSize.Width;
            double resultHeight = destSize.Height;

            if (ratio * destSize.Height > destSize.Width)
            {
                resultHeight = destSize.Width / ratio;
            }
            else
            {
                resultWidth = destSize.Height * ratio;
            }

            return new Size(resultWidth, resultHeight);
        }

        public static Rect ReduceRectangle(Rect displayRect, Rect cutout)
        {
            double left = Math.Min(1, Math.Max(0, cutout.Left)) * displayRect.Width + displayRect.Left;
            double right = Math.Min(1, Math.Max(0, cutout.Right)) * displayRect.Width + displayRect.Left;

            double top = Math.Min(1, Math.Max(0, cutout.Top)) * displayRect.Height + displayRect.Top;
            double bottom = Math.Min(1, Math.Max(0, cutout.Bottom)) * displayRect.Height + displayRect.Top;

            return new Rect(new Point(left, top), new Point(right, bottom));
        }
    }
}
