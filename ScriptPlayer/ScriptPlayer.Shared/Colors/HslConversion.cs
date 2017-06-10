using System;

namespace ScriptPlayer.Shared
{
    public static class HslConversion
    {
        public static Tuple<double,double,double> FromRgb(Byte red, Byte green, Byte blue)
        {
            double r = red / 255.0;
            double g = green / 255.0;
            double b = blue / 255.0;

            double min = Math.Min(Math.Min(r, g), b);
            double max = Math.Max(Math.Max(r, g), b);
            double delta = max - min;

            double h = 0;
            double s = 0;
            double l = (max + min) / 2.0;

            if (delta != 0)
            {
                if (l < 0.5f)
                {
                    s = delta / (max + min);
                }
                else
                {
                    s = delta / (2.0f - max - min);
                }


                if (r == max)
                {
                    h = (g - b) / delta;
                }
                else if (g == max)
                {
                    h = 2 + (b - r) / delta;
                }
                else if (b == max)
                {
                    h = 4 + (r - g) / delta;
                }
            }

            return new Tuple<double, double, double>(h * 100.0, s * 100.0, l * 100.0);
        }
    }
}
