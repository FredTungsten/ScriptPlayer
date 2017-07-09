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
            _condition = new MajoityPixelCondition(Color.FromRgb(165, 90, 238), 51);
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

            if (!_colorsByTime.Any(i => i.Item1 == timestamp))
            {
                _colorsByTime.Add(new Tuple<TimeSpan, Color>(timestamp, average));

                while (_colorsByTime.Count > 500)
                    _colorsByTime.RemoveAt(0);
            }

            bool active = _condition.CheckSample(rgbPixels);

            if (active && !_active)
            {
                OnBeatDetected(timestamp);
            }
            _active = active;

            PixelPreview = new SolidColorBrush(average);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnBeatDetected(TimeSpan e)
        {
            BeatDetected?.Invoke(this, e);
        }

        public void Refresh()
        {
            RefreshSample();
        }
    }

    public enum ConditionSource
    {
        Average,
        Majority
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
        public ConditionSource Source { get; set; }
        public SampleCondtionParameter MatchedSamples { get; set; }
        public SampleCondtionParameter Red { get; set; }
        public SampleCondtionParameter Green { get; set; }
        public SampleCondtionParameter Blue { get; set; }
        public SampleCondtionParameter Hue { get; set; }
        public SampleCondtionParameter Saturation { get; set; }
        public SampleCondtionParameter Luminosity { get; set; }
        public override bool CheckSample(byte[] rgbPixels)
        {
            switch (Source)
            {
                case ConditionSource.Average:
                    Color averageColor = GetAverageColor(rgbPixels);
                    return CheckSample(averageColor.R, averageColor.G, averageColor.B);
                case ConditionSource.Majority:
                    int pixels = 0;
                    int positiveSamples = 0;

                    for (int i = 0; i < rgbPixels.Length - 3; i += 3)
                    {
                        pixels++;
                        positiveSamples += CheckSample(rgbPixels[i + 0], rgbPixels[i + 1], rgbPixels[i + 2]) ? 1 : 0;
                    }

                    int acceptedPixels = (int) Math.Round(100.0 * (positiveSamples / (double) pixels));

                    return MatchedSamples.IsAcceptableValue(acceptedPixels);
            }

            return false;
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

    public class MinSaturationCondition : SampleCondition
    {
        public MinSaturationCondition(byte minSaturation)
        {
            MinSaturation = minSaturation;
        }

        public byte MinSaturation { get; set; }
        public override bool CheckSample(byte[] rgbPixels)
        {
            return GetSampleScoreRaw(rgbPixels) >= MinSaturation;
        }

        public double GetSampleScoreRaw(byte[] rgbPixels)
        {
            Color c = GetAverageColor(rgbPixels);
            var hsl = HslConversion.FromRgb(c.R, c.G, c.B);
            return hsl.Item2;
        }
    }

    public class MinBrightnessCondition : SampleCondition
    {
        public MinBrightnessCondition(byte minBrightness)
        {
            MinBrightness = minBrightness;
        }

        public byte MinBrightness { get; set; }
        public override bool CheckSample(byte[] rgbPixels)
        {
            return GetSampleScoreRaw(rgbPixels) >= MinBrightness * 3;
        }

        public double GetSampleScoreRaw(byte[] rgbPixels)
        {
            Color c = GetAverageColor(rgbPixels);
            return c.R + c.G + c.B;
        }
    }

    public abstract class SampleCondition
    {
        public abstract bool CheckSample(byte[] rgbPixels);

        public virtual bool CheckSample(System.Drawing.Color[] pixels)
        {
            byte[] sample = new byte[pixels.Length * 3];

            for (int i = 0; i < pixels.Length; i++)
            {
                sample[4 * i + 0] = pixels[i].R;
                sample[4 * i + 1] = pixels[i].G;
                sample[4 * i + 2] = pixels[i].B;
            }

            return CheckSample(sample);
        }

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

    public class MajoityPixelCondition : SampleCondition
    {
        private byte _r;
        private double _factorMin;
        private byte _g;
        private byte _b;
        private byte _offset;

        public Color ReferenceColor => Color.FromRgb(_r, _g, _b);
        public double PercentageMin => _factorMin * 100.0;
        public byte MaxOffset => _offset;

        public MajoityPixelCondition(Color referenceColor, double percentageMin, byte maxOffset = 10)
        {
            _r = referenceColor.R;
            _g = referenceColor.G;
            _b = referenceColor.B;
            _offset = maxOffset;
            _factorMin = percentageMin / 100.0;
        }

        public override bool CheckSample(byte[] pixels)
        {
            return GetSampleScoreRaw(pixels) >= _factorMin;
        }

        public double GetSampleScoreRaw(byte[] pixels)
        {
            int positive = 0;
            int total = 0;

            for (int i = 0; i < pixels.Length; i += 3)
            {
                if (IsSimilar(pixels[i + 0], pixels[i + 1], pixels[i + 2], _offset))
                    positive++;
                total++;
            }

            return (double)positive / total;
        }

        private bool IsSimilar(byte r, byte g, byte b, byte maxOffset)
        {
            if (Math.Abs(_r - r) > maxOffset) return false;
            if (Math.Abs(_g - g) > maxOffset) return false;
            return Math.Abs(_b - b) <= maxOffset;
        }
    }

    public class AverageColorCondition : SampleCondition
    {
        private byte _r;
        private byte _g;
        private byte _b;
        private byte _offset;

        public Color ReferenceColor => Color.FromRgb(_r, _g, _b);
        public byte MaxOffset => _offset;

        public AverageColorCondition(Color referenceColor, byte maxOffset)
        {
            _r = referenceColor.R;
            _g = referenceColor.G;
            _b = referenceColor.B;
            _offset = maxOffset;
        }

        public override bool CheckSample(byte[] pixels)
        {
            return GetSampleScoreRaw(pixels) <= _offset;
        }

        public double GetSampleScoreRaw(byte[] pixels)
        {
            Color average = GetAverageColor(pixels);
            byte maxOffset = Similarity(average, _r, _g, _b);
            return maxOffset;
        }
    }
}
