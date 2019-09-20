using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using ScriptPlayer.Shared.Properties;

namespace ScriptPlayer.Shared
{
    public class ColorSampler : INotifyPropertyChanged
    {
        public event EventHandler<TimeSpan> BeatDetected;

        private TimeSource _timeSource;
        private Brush _source;
        private DrawingVisual _drawing;
        private RenderTargetBitmap _bitmap;
        private Resolution _resolution;
        private bool _active;
        private Brush _pixelPreview;

        public List<Tuple<TimeSpan, Color>> _colorsByTime = new List<Tuple<TimeSpan, Color>>();

        public TimeSource TimeSource
        {
            get { return _timeSource; }
            set
            {
                if (ReferenceEquals(_timeSource, value)) return;

                if (_timeSource != null)
                    _timeSource.ProgressChanged -= ValueOnProgressChanged;

                if (value != null)
                    value.ProgressChanged += ValueOnProgressChanged;

                _timeSource = value;
            }
        }

        public Brush PixelPreview
        {
            get { return _pixelPreview; }
            set
            {
                if (Equals(value, _pixelPreview)) return;
                _pixelPreview = value;
                OnPropertyChanged();
            }
        }

        public Resolution Resolution
        {
            get { return _resolution; }
            set { _resolution = value; }
        }

        public Brush Source
        {
            get { return _source; }
            set
            {
                if (ReferenceEquals(value, _source)) return;

                _source = value;

                RefreshSample();
            }
        }

        private void RefreshSample()
        {
            _bitmap = new RenderTargetBitmap(Sample.Width, Sample.Height, 96, 96, PixelFormats.Pbgra32);

            _drawing = new DrawingVisual();

            using (var dc = _drawing.RenderOpen())
                dc.DrawRectangle(_source, null, new Rect(-Sample.X, -Sample.Y, Resolution.Horizontal, Resolution.Vertical));
        }

        public Int32Rect Sample
        {
            get { return _sample; }
            set
            {
                _sample = value;
                RefreshSample();
            }
        }

        public ColorSampler()
        {
            _condition = new PixelColorSampleCondition();
        }

        private void ValueOnProgressChanged(object sender, TimeSpan d)
        {
            TakeSample(d);
        }

        public List<Color> GetColors(TimeSpan maxTimeStamp, int maxCount)
        {
            return _colorsByTime.Where(p => p.Item1 <= maxTimeStamp).OrderByDescending(p => p.Item1).Take(maxCount).Select(p => p.Item2).ToList();
        }

        private TimeSpan lastTimestamp;
        private Int32Rect _sample;

        private SampleCondition _condition;

        private void TakeSample(TimeSpan timestamp)
        {
            if (timestamp == lastTimestamp)
                return;

            lastTimestamp = timestamp;

            _bitmap.Render(_drawing);

            var rect = Sample;

            byte[] pixel = new byte[rect.Width * rect.Height * 4];

            _bitmap.CopyPixels(new Int32Rect(0, 0, Sample.Width, Sample.Height), pixel, rect.Width * 4, 0);

            byte[] rgbPixels = SampleCondition.ConvertBgraToRgb(pixel);

            Color average = SampleCondition.GetAverageColor(rgbPixels);

            if (_colorsByTime.All(i => i.Item1 != timestamp))
            {
                _colorsByTime.Add(new Tuple<TimeSpan, Color>(timestamp, average));

                while (_colorsByTime.Count > 500)
                    _colorsByTime.RemoveAt(0);
            }

            PixelPreview = new SolidColorBrush(average);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Refresh()
        {
            RefreshSample();
        }
    }

    public enum ConditionType
    {
        Absolute,
        Relative
    }
    public enum ConditionState
    {
        NotUsed,
        Include,
        Exclude
    }

    public class SampleCondtionParameter
    {
        [XmlAttribute("Min")]
        public int MinValue { get; set; }
        [XmlAttribute("Max")]
        public int MaxValue { get; set; }
        [XmlAttribute("State")]
        public ConditionState State { get; set; }


        public bool IsAcceptableValue(int value)
        {
            switch (State)
            {
                case ConditionState.NotUsed:
                {
                    return true;
                }
                case ConditionState.Include:
                {
                    if (MinValue > value) return false;
                    if (MaxValue < value) return false;
                    return true;
                }
                case ConditionState.Exclude:
                {
                    if (value < MinValue) return true;
                    if (value > MaxValue) return true;
                    return false;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class PixelColorSampleCondition : SampleCondition
    {
        public ConditionType Type { get; set; }
        
        public SampleCondtionParameter Red { get; set; }
        public SampleCondtionParameter Green { get; set; }
        public SampleCondtionParameter Blue { get; set; }
        public SampleCondtionParameter Hue { get; set; }
        public SampleCondtionParameter Saturation { get; set; }
        public SampleCondtionParameter Luminosity { get; set; }

        public override bool CheckSample(byte[] rgbPixels, byte[] previousPixels)
        {
            switch (Type)
            {
                case ConditionType.Absolute:
                {
                    Color averageColor = GetAverageColor(rgbPixels);
                    return CheckSample(averageColor.R, averageColor.G, averageColor.B);
                }
                case ConditionType.Relative:
                {
                    Color color1 = GetAverageColor(rgbPixels);
                    Color color2 = GetAverageColor(previousPixels);
                    return CheckSample(color1.R, color1.G, color1.B, color2.R, color2.G, color2.B);
                }
            }

            return false;
        }

        private bool CheckSample(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
        {
            if (Red == null) return false;

            if (!Red.IsAcceptableValue(Math.Abs(r1-r2))) return false;
            if (!Green.IsAcceptableValue(Math.Abs(g1 - g2))) return false;
            if (!Blue.IsAcceptableValue(Math.Abs(b1 - b2))) return false;

            if (Hue.State != ConditionState.NotUsed || Saturation.State != ConditionState.NotUsed ||
                Luminosity.State != ConditionState.NotUsed)
            {
                var hsl1 = HslConversion.FromRgb(r1, g1, b1);
                var hsl2 = HslConversion.FromRgb(r2, g2, b2);

                var hsl = new Tuple<double, double, double>(Math.Abs(hsl1.Item1 - hsl2.Item1),
                    Math.Abs(hsl1.Item2 - hsl2.Item2), Math.Abs(hsl1.Item3 - hsl2.Item3));

                if (hsl.Item3 > 180)
                    hsl = new Tuple<double, double, double>(hsl.Item1, hsl.Item2, 360 - hsl.Item3);

                if (!Hue.IsAcceptableValue((int)Math.Round(hsl.Item1))) return false;
                if (!Saturation.IsAcceptableValue((int)Math.Round(hsl.Item2))) return false;
                if (!Luminosity.IsAcceptableValue((int)Math.Round(hsl.Item3))) return false;
            }

            return true;
        }

        private bool CheckSample(byte r, byte g, byte b)
        {
            if (Red == null) return false;

            if (!Red.IsAcceptableValue(r)) return false;
            if (!Green.IsAcceptableValue(g)) return false;
            if (!Blue.IsAcceptableValue(b)) return false;

            if (Hue.State != ConditionState.NotUsed || Saturation.State != ConditionState.NotUsed ||
                Luminosity.State != ConditionState.NotUsed)
            {
                var hsl = HslConversion.FromRgb(r, g, b);

                if (!Hue.IsAcceptableValue((int)Math.Round(hsl.Item1))) return false;
                if (!Saturation.IsAcceptableValue((int)Math.Round(hsl.Item2))) return false;
                if (!Luminosity.IsAcceptableValue((int)Math.Round(hsl.Item3))) return false;
            }

            return true;
        }
    }

    public abstract class SampleCondition
    {
        public abstract bool CheckSample(byte[] rgbPixels, byte[] previousPixels);

        public static Color GetAverageColor(byte[] pixels)
        {
            int c = 0;
            int r = 0;
            int g = 0;
            int b = 0;

            for (int i = 0; i < pixels.Length; i += 3)
            {
                c++;
                r += pixels[i + 0];
                g += pixels[i + 1];
                b += pixels[i + 2];
            }

            return Color.FromArgb((byte)255, (byte)(r / c), (byte)(g / c), (byte)(b / c));
        }

        public static byte Similarity(Color color1, Color color2)
        {
            return (byte)Math.Max(Math.Abs(color1.R - color2.R),
                Math.Max(Math.Abs(color1.G - color2.G), Math.Abs(color1.B - color2.B)));
        }

        public static byte Similarity(Color color, byte R, byte G, byte B)
        {
            return (byte)Math.Max(Math.Abs(color.R - R),
                Math.Max(Math.Abs(color.G - G),
                    Math.Abs(color.B - B)));
        }

        public static byte Similarity(byte R1, byte R2, byte G1, byte G2, byte B1, byte B2)
        {
            return (byte)Math.Max(Math.Abs(R1 - R2),
                Math.Max(Math.Abs(G1 - G2),
                    Math.Abs(B1 - B2)));
        }

        public static byte[] ConvertBgraToRgb(byte[] pixel)
        {
            int pixels = pixel.Length / 4;
            byte[] rgbPixels = new byte[pixels * 3];

            for (int i = 0; i < pixels; i++)
            {
                rgbPixels[3 * i + 0] = pixel[4 * i + 2];
                rgbPixels[3 * i + 1] = pixel[4 * i + 1];
                rgbPixels[3 * i + 2] = pixel[4 * i + 0];
            }

            return rgbPixels;
        }
    }
}
