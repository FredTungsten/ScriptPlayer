using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public static class HeatMapGenerator
    {
        public static GradientStopCollection HeatMap;
        public static GradientStopCollection HeatMap2;

        static HeatMapGenerator()
        {
            HeatMap = GradientsSmoothFromColors(0.5, Colors.Blue, Colors.Cyan, Colors.Lime, Colors.Yellow, Colors.Red);
            HeatMap2 = GradientsSmoothFromColors(0.5, Colors.Black, Colors.Cyan, Colors.Lime, Colors.Yellow, Colors.Red);
            HeatMap2.Insert(1,new GradientStop(Colors.Blue, 0.01));

        }

        public static GradientStopCollection GradientsSmoothFromColors(double fade, params Color[] colors)
        {
            fade = 0.5 - Math.Min(0.5, Math.Max(0, fade));
            GradientStopCollection gradients = new GradientStopCollection();

            for (int i = 0; i < colors.Length; i++)
            {
                if(i > 0)
                    gradients.Add(new GradientStop(colors[i], (i-fade) / (colors.Length - 1)));

                gradients.Add(new GradientStop(colors[i], (double)i / (colors.Length - 1)));

                if (i < colors.Length - 1)
                    gradients.Add(new GradientStop(colors[i], (i+fade) / (colors.Length - 1)));
            }

            return gradients;
        }

        public static GradientStopCollection GradientsSharpFromColors(params Color[] colors)
        {
            GradientStopCollection gradients = new GradientStopCollection();

            for (int i = 0; i < colors.Length; i++)
            {
                gradients.Add(new GradientStop(colors[i], i / (double)colors.Length));
                gradients.Add(new GradientStop(colors[i], (i + 1) / (double)colors.Length));
            }

            return gradients;
        }

        public static Color GetColorAtPosition(GradientStopCollection heatMap, double value)
        {
            for (int i = 0; i < heatMap.Count; i++)
            {
                if (heatMap[i].Offset > value)
                {
                    if (i == 0)
                        return heatMap[i].Color;

                    double ratio = Lerp(value, heatMap[i - 1].Offset, heatMap[i].Offset);
                    return MixColors(heatMap[i - 1].Color, heatMap[i].Color, ratio);
                }
            }

            return heatMap[heatMap.Count - 1].Color;
        }

        private static Color MixColors(Color color1, Color color2, double ratio)
        {
            float a = (float) (color2.ScA * ratio + color1.ScA * (1.0 - ratio));
            float r = (float) (color2.ScR * ratio + color1.ScR * (1.0 - ratio));
            float g = (float) (color2.ScG * ratio + color1.ScG * (1.0 - ratio));
            float b = (float) (color2.ScB * ratio + color1.ScB * (1.0 - ratio));
            return Color.FromScRgb(a, r, g, b);
        }

        private static double Lerp(double value, double min, double max)
        {
            double val = (value - min) / (max - min);
            return Math.Min(1, Math.Max(0, val));
        }

        public static Brush Generate(List<TimeSpan> beats, TimeSpan timeFrom, TimeSpan timeTo, int segments, bool smooth, double fade = 0.2)
        {
            int[] beatsPerSegment = new int[segments];

            TimeSpan duration = timeTo - timeFrom;
            TimeSpan segmentLength = duration.Divide(segments);

            foreach (TimeSpan beat in beats)
            {
                if (beat < timeFrom || beat > timeTo) continue;

                double position = (beat - timeFrom).Divide(segmentLength);
                int index = (int) position;
                if (index >= beatsPerSegment.Length)
                    index = beatsPerSegment.Length - 1;
                beatsPerSegment[index] = beatsPerSegment[index] + 1;
            }

            int max = beatsPerSegment.Max();

            var colors = beatsPerSegment
                .Select(v => GetColorAtPosition(HeatMap2, v / (double) max)).ToArray();

            GradientStopCollection gradients = smooth ? GradientsSmoothFromColors(fade, colors) : GradientsSharpFromColors(colors);

            LinearGradientBrush brush = new LinearGradientBrush(gradients,new Point(0,0), new Point(1,0));
            brush.MappingMode = BrushMappingMode.RelativeToBoundingBox;
            
            return brush;
        }
    }
}
