using System;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public static class HslConversion
    {
        public static Tuple<byte, byte, byte> FromHsl(double hue, double saturation, double luminosity)
        {
            saturation /= 100.0;
            luminosity /= 100.0;
            hue /= 360.0;

            double var1, var2;
            byte r, g, b;

            if (saturation == 0)
            {
                r = (Byte)(luminosity * 255);
                g = (Byte)(luminosity * 255);
                b = (Byte)(luminosity * 255);
            }
            else
            {
                if (luminosity < 0.5)
                    var2 = luminosity * (1 + saturation);
                else
                    var2 = (luminosity + saturation) - (saturation * luminosity);

                var1 = 2 * luminosity - var2;

                r = (Byte)(255 * HueToRgbValue(var1, var2, hue + (1 / 3.0)));
                g = (Byte)(255 * HueToRgbValue(var1, var2, hue));
                b = (Byte)(255 * HueToRgbValue(var1, var2, hue - (1 / 3.0)));
            }

            return new Tuple<byte, byte, byte>(r,g,b);
        }

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
                    if (h < 0)
                        h += 6;
                }
                else if (g == max)
                {
                    h = 2 + (b - r) / delta;
                }
                else if (b == max)
                {
                    h = 4 + (r - g) / delta;
                }

                h *= 60;
            }

            return new Tuple<double, double, double>(h, s * 100.0, l * 100.0);
        }

        private static double HueToRgbValue(double v1, double v2, double vH)
        {
            if (vH < 0) vH += 1;
            if (vH > 1) vH -= 1;
            if ((6 * vH) < 1) return (v1 + (v2 - v1) * 6 * vH);
            if ((2 * vH) < 1) return (v2);
            if ((3 * vH) < 2) return (v1 + (v2 - v1) * ((2.0 / 3) - vH) * 6);
            return (v1);
        }

        public static Color Blend(Color colorA, Color colorB, double progress)
        {
            var hslA = FromRgb(colorA.R, colorA.G, colorA.B);
            var hslB = FromRgb(colorB.R, colorB.G, colorB.B);

            double hue = BlendHue(hslA.Item1, hslB.Item1, progress);
            double saturation = hslA.Item2 * (1.0 - progress) + hslB.Item2 * progress;
            double luminosity = hslA.Item3 * (1.0 - progress) + hslB.Item3 * progress;
            byte alpha = (byte) Math.Min(255,
                Math.Max(0, Math.Round(colorA.A * (1.0 - progress) + colorB.A * progress)));

            var rgb = FromHsl(hue, saturation, luminosity);
            return Color.FromArgb(alpha, rgb.Item1, rgb.Item2, rgb.Item3);
        }

        private static double BlendHue(double hA, double hB, double progress)
        {
            double distance;

            if (hA < hB)
            {
                if (hB - hA <= 180)
                    distance = hB - hA;
                else
                    distance = (hB - hA) - 360;
            }
            else
            {
                if (hA - hB <= 180)
                    distance = hB - hA;
                else
                    distance = hB - hA + 360;
            }

            double value = hA + progress * distance;
            while (value >= 360)
                value -= 360;
            while (value < 0)
                value += 360;

            return value;
        }
    }
}
