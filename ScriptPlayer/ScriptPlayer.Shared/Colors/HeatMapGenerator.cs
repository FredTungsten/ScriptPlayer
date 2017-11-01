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
            HeatMap = GradientsSmoothFromColors(0.5, Colors.DodgerBlue, Colors.Cyan, Colors.Lime, Colors.Yellow, Colors.Red);
            HeatMap2 = GradientsSmoothFromColors(0.5, Colors.Black, Colors.Cyan, Colors.Lime, Colors.Yellow,
                Colors.Red);
            HeatMap2.Insert(1, new GradientStop(Colors.DodgerBlue, 0.01));
        }

        public static GradientStopCollection GradientsSmoothFromColors(double fade, params Color[] colors)
        {
            fade = 0.5 - Math.Min(0.5, Math.Max(0, fade));
            GradientStopCollection gradients = new GradientStopCollection();

            for (int i = 0; i < colors.Length; i++)
            {
                if (i > 0)
                    gradients.Add(new GradientStop(colors[i], (i - fade) / (colors.Length - 1)));

                gradients.Add(new GradientStop(colors[i], (double) i / (colors.Length - 1)));

                if (i < colors.Length - 1)
                    gradients.Add(new GradientStop(colors[i], (i + fade) / (colors.Length - 1)));
            }

            return gradients;
        }

        public static GradientStopCollection GradientsSharpFromColors(params Color[] colors)
        {
            GradientStopCollection gradients = new GradientStopCollection();

            for (int i = 0; i < colors.Length; i++)
            {
                gradients.Add(new GradientStop(colors[i], i / (double) colors.Length));
                gradients.Add(new GradientStop(colors[i], (i + 1) / (double) colors.Length));
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

        public static Brush Generate(List<TimeSpan> beats, TimeSpan timeFrom, TimeSpan timeTo, int segments,
            bool smooth, double fade = 0.2)
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

            int max = (int) (segmentLength.TotalSeconds * 5);

            var colors = beatsPerSegment
                .Select(v => GetColorAtPosition(HeatMap2, v / (double) max)).ToArray();

            GradientStopCollection gradients = smooth
                ? GradientsSmoothFromColors(fade, colors)
                : GradientsSharpFromColors(colors);

            LinearGradientBrush brush = new LinearGradientBrush(gradients, new Point(0, 0), new Point(1, 0));
            brush.MappingMode = BrushMappingMode.RelativeToBoundingBox;

            return brush;
        }

        public static Brush Generate2(List<TimeSpan> beats, TimeSpan timeFrom, TimeSpan timeTo)
        {
            List<List<TimeSpan>> segments = new List<List<TimeSpan>>();
            
            TimeSpan previous = timeFrom;
            TimeSpan isGap = TimeSpan.FromSeconds(10);
            TimeSpan fastest = TimeSpan.FromMilliseconds(200);
            TimeSpan duration = timeTo - timeFrom;

            foreach (TimeSpan beat in beats)
            {
                if (beat < timeFrom || beat > timeTo) continue;

                if (beat - previous >= isGap)
                {
                    segments.Add(new List<TimeSpan>());
                }

                if(segments.Count == 0)
                    segments.Add(new List<TimeSpan>());

                segments.Last().Add(beat);
                previous = beat;
            }

            GradientStopCollection stops = new GradientStopCollection();
            stops.Add(new GradientStop(Colors.Black, 0.0));

            foreach (List<TimeSpan> segment in segments)
            {
                if (segment.Count == 1)
                {
                    TimeSpan stamp = segment.Single();
                    TimeSpan span = TimeSpan.FromSeconds(2);

                    TimeSpan beginSegment = stamp - span;
                    if (beginSegment < timeFrom)
                        beginSegment = timeFrom;

                    TimeSpan endSegment = stamp + span;
                    if (endSegment > timeTo)
                        endSegment = timeTo;

                    stops.Add(new GradientStop(Colors.Black, beginSegment.Divide(duration)));
                    stops.Add(new GradientStop(Colors.DodgerBlue, beginSegment.Divide(duration)));
                    stops.Add(new GradientStop(Colors.DodgerBlue, endSegment.Divide(duration)));
                    stops.Add(new GradientStop(Colors.Black, endSegment.Divide(duration)));
                }
                else
                { 
                TimeSpan span = segment.Last() - segment.First();
                int segmentCount = (int) Math.Max(1, Math.Min((segment.Count -1) / 12.0, span.Divide(duration.Divide(200))));

                stops.Add(new GradientStop(Colors.Black, (segment.First() - timeFrom).Divide(duration)));

                for (int i = 0; i < segmentCount; i++)
                {
                    int startIndex = Math.Min(segment.Count - 1,(int)((i * (segment.Count-1)) / (double)segmentCount));
                    int endIndex = Math.Min(segment.Count - 1, (int)(((i+1) * (segment.Count-1)) / (double)segmentCount));
                    int beatCount = endIndex - startIndex - 1;

                    TimeSpan firstBeat = segment[startIndex];
                    TimeSpan lastBeat = segment[endIndex];

                    TimeSpan averageLength = (lastBeat - firstBeat).Divide(beatCount);
                    double value = fastest.Divide(averageLength);

                    value = Math.Min(1, Math.Max(0, value));
                    Color color = GetColorAtPosition(HeatMap, value);

                    double positionStart = firstBeat.Divide(duration);
                    double positionEnd = lastBeat.Divide(duration);

                    if(i == 0)
                        stops.Add(new GradientStop(color, positionStart));

                    stops.Add(new GradientStop(color, (positionEnd + positionStart)/2.0));

                    if(i == segmentCount - 1)
                        stops.Add(new GradientStop(color, positionEnd));
                }
                stops.Add(new GradientStop(Colors.Black, (segment.Last() - timeFrom).Divide(duration)));}
            }

            stops.Add(new GradientStop(Colors.Black, 1.0));

            LinearGradientBrush brush = new LinearGradientBrush(stops, new Point(0, 0), new Point(1, 0));
            brush.MappingMode = BrushMappingMode.RelativeToBoundingBox;

            return brush;
        }
    }
}
