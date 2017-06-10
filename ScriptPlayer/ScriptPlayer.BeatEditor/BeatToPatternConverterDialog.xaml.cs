using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Dialogs;

namespace LaunchControl.BeatEditor
{
    /// <summary>
    /// Interaction logic for BeatToPatternConverterDialog.xaml
    /// </summary>
    public partial class BeatToPatternConverterDialog : Window
    {
        public static readonly DependencyProperty BeatsProperty = DependencyProperty.Register(
            "Beats", typeof(BeatCollection), typeof(BeatToPatternConverterDialog), new PropertyMetadata(default(BeatCollection)));

        public BeatCollection Beats
        {
            get { return (BeatCollection)GetValue(BeatsProperty); }
            set { SetValue(BeatsProperty, value); }
        }

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Duration", typeof(TimeSpan), typeof(BeatToPatternConverterDialog), new PropertyMetadata(default(TimeSpan)));

        public TimeSpan Duration
        {
            get { return (TimeSpan)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public static readonly DependencyProperty ViewPortProperty = DependencyProperty.Register(
            "ViewPort", typeof(TimeSpan), typeof(BeatToPatternConverterDialog), new PropertyMetadata(default(TimeSpan), OnPositionRelevantPropertyChanged));

        private static void OnPositionRelevantPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BeatToPatternConverterDialog)d).UpdateOffset();
        }

        private void UpdateOffset()
        {
            Offset = Position - ViewPort.Divide(2);
        }

        public TimeSpan ViewPort
        {
            get { return (TimeSpan)GetValue(ViewPortProperty); }
            set { SetValue(ViewPortProperty, value); }
        }

        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            "Position", typeof(TimeSpan), typeof(BeatToPatternConverterDialog), new PropertyMetadata(default(TimeSpan), OnPositionRelevantPropertyChanged));

        public TimeSpan Position
        {
            get { return (TimeSpan)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
            "Offset", typeof(TimeSpan), typeof(BeatToPatternConverterDialog), new PropertyMetadata(default(TimeSpan)));

        private TimeSpan _begin;
        private TimeSpan _end;

        public TimeSpan Offset
        {
            get { return (TimeSpan)GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        public static readonly DependencyProperty SegmentsProperty = DependencyProperty.Register(
            "Segments", typeof(ObservableCollection<BeatSegment>), typeof(BeatToPatternConverterDialog), new PropertyMetadata(default(ObservableCollection<BeatSegment>)));

        public ObservableCollection<BeatSegment> Segments
        {
            get { return (ObservableCollection<BeatSegment>) GetValue(SegmentsProperty); }
            set { SetValue(SegmentsProperty, value); }
        }

        public BeatToPatternConverterDialog()
        {
            Segments = new ObservableCollection<BeatSegment>();
            InitializeComponent();
        }

        private void btnLoadBeatsFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { Filter = "Text Files|*.txt" };
            if (dialog.ShowDialog(this) != true) return;

            Beats = BeatCollection.Load(dialog.FileName);
            Duration = Beats.Max();
            UpdateHeatMap();
        }

        private void lsdPosition_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Position = Duration.Multiply(((Slider)sender).Value);
        }

        private void sldViewPort_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ViewPort = TimeSpan.FromMilliseconds(((Slider)sender).Value);
        }

        private void UpdateHeatMap()
        {
            if (rectHeat == null) return;
            rectHeat.Fill = HeatMapGenerator.Generate(Beats.ToList(), TimeSpan.Zero, Duration, 100, true);
        }

        private void btnMarkBegin_Click(object sender, RoutedEventArgs e)
        {
            _begin = Position;
        }

        private void btnMarkEnd_Click(object sender, RoutedEventArgs e)
        {
            _end = Position;
        }

        private void btnDetect_Click(object sender, RoutedEventArgs e)
        {
            BeatPatternEditor editor =
                new BeatPatternEditor(new bool[] { true, false, false, false, false, false, false, false, });

            if (editor.ShowDialog() != true) return;

            bool[] pattern = editor.Result;
            var beats = Beats.GetBeats(_begin, _end).ToList();

            BeatSegment segment = GetSegment(beats, pattern);
            if (segment == null) return;

            Segments.Add(segment);
        }

        private BeatSegment GetSegment(List<TimeSpan> beats, bool[] pattern)
        {
            int beatsPerSequence = pattern.Count(b => b);
            if (beatsPerSequence == 0) return null;

            int totalBeats = beats.Count;
            int fullSequences = totalBeats / beatsPerSequence;

            if (fullSequences == 0) return null;

            List<TimeSpan> workingBeats = beats.Take(fullSequences * beatsPerSequence).ToList();

            TimeSpan firstBeat = workingBeats.Min();
            TimeSpan lastBeat = workingBeats.Max();

            double lastBeatDurationFactor = CalculateLastBeatDurationFactor(pattern);

            double extendedFullSequences = fullSequences - lastBeatDurationFactor;

            TimeSpan duration = lastBeat - firstBeat;
            TimeSpan patternDuration = duration.Divide(extendedFullSequences);


            if (patternDuration == TimeSpan.Zero) return null;

            return new BeatSegment
            {
                Beat = new BeatDefinition { Pattern = pattern },
                Duration = patternDuration.Multiply(fullSequences).Ticks,
                PatternDuration = patternDuration.Ticks,
                Position = firstBeat.Ticks
            };
        }

        private double CalculateLastBeatDurationFactor(bool[] pattern)
        {
            int l = 0;
            for (int i = 0; i < pattern.Length; i++)
                if (pattern[i])
                    l = i;

            return 1 - ((double)l / pattern.Length);
        }


        private TimeSpan EstimateBeatDuration(List<TimeSpan> pauses, TimeSpan maxDerivation)
        {
            TimeSpan duration = TimeSpan.FromTicks(pauses.Sum(b => b.Ticks));

            TimeSpan bestFit = TimeSpan.Zero;
            TimeSpan bestFitDerivation = duration;


            int count = pauses.Count;

            for (int divider = count; divider <= count * 8; divider++)
            {
                TimeSpan beatLength = duration.Divide(divider);

                TimeSpan derivation = CheckDerivation(pauses, beatLength);
                if (derivation > maxDerivation) continue;

                if (bestFitDerivation > derivation)
                {
                    bestFit = beatLength;
                    bestFitDerivation = derivation;
                }
            }

            return bestFit;
        }

        private TimeSpan CheckDerivation(List<TimeSpan> pauses, TimeSpan beatLength)
        {
            TimeSpan maxDerivation = TimeSpan.Zero;

            foreach (TimeSpan pause in pauses)
            {
                double factor = pause.Divide(beatLength);
                int fullFactor = (int)Math.Round(factor);
                double derivation = Math.Abs(factor - fullFactor);
                TimeSpan deriavtionSpan = beatLength.Multiply(derivation);

                if (deriavtionSpan > maxDerivation)
                    maxDerivation = deriavtionSpan;
            }

            return maxDerivation;
        }

        private List<TimeSpan> GetPauses(List<TimeSpan> beats)
        {
            List<TimeSpan> result = new List<TimeSpan>();
            for (int i = 1; i < beats.Count; i++)
            {
                result.Add(beats[i] - beats[i - 1]);
            }
            return result;
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            Segments.Clear();
        }

        private void btnDetectAll_Click(object sender, RoutedEventArgs e)
        {
            List<bool[]> knownPatterns = new List<bool[]>
            {
                new bool[]{true, false, false, false, false, false, false, false},
                new bool[]{true, false, false, false, true, false, false, false},
                new bool[]{true, false, true, false, true, false, true, false},
                new bool[]{true, true, true, true, true, true, true, true},
                new bool[]{true, true, true, false, true, false, true, false},
                new bool[]{true, true, true, true, true, false, true, false},
                new bool[]{true, true, true, true, true, true, true, false},
                new bool[]{true, true, false,false,false,false,false,false}
            };

            List<TimeSpan> beats = Beats.ToList();
            int index = 0;

            BeatSegment segment;
            do
            {
                segment = null;
                int shifts = 0;

                while (segment == null && shifts < 8)
                {
                    segment = FindSegment(knownPatterns, beats, ref index);
                    if (segment == null)
                    {
                        shifts++;
                        index++;
                    }
                }

                if (segment != null)
                {
                    Segments.Add(segment);
                }
            }while(segment != null);
        }

        private BeatSegment FindSegment(List<bool[]> knownPatterns, List<TimeSpan> beats, ref int index)
        {
            if (index < 0) return null;
            if (index >= beats.Count) return null;

            int initialIndex = index;
            bool[] bestPattern = null;
            int bestPatternBeats = 0;


            foreach (bool[] pattern in knownPatterns)
            {
                int intermediateIndex = index;
                int patternBeats = pattern.Count(b => b);
                List<TimeSpan> samples = beats.Skip(intermediateIndex).Take(patternBeats).ToList();


                int matchedBeats = 0;

                while (MatchBeat(samples, pattern))
                {
                    intermediateIndex += patternBeats;
                    if (intermediateIndex >= beats.Count)
                        break;

                    matchedBeats += patternBeats;

                    samples.AddRange(beats.Skip(intermediateIndex).Take(patternBeats));
                }

                if (matchedBeats > bestPatternBeats)
                {
                    bestPattern = pattern;
                    bestPatternBeats = matchedBeats;
                }
            }

            if (bestPattern == null)
                return null;

            index += bestPatternBeats;

            return GetSegment(beats.Skip(initialIndex).Take(bestPatternBeats).ToList(), bestPattern);
        }

        private bool MatchBeat(List<TimeSpan> samples, bool[] pattern)
        {
            int patternBeats = pattern.Count(b => b);
            int sampleBeats = samples.Count;
            double repeats = sampleBeats / (double)patternBeats - CalculateLastBeatDurationFactor(pattern);

            TimeSpan duration = samples.Max() - samples.Min();
            TimeSpan patternDuration = duration.Divide(repeats);

            List<TimeSpan> durations = CalculateDurations(pattern, patternDuration);

            int sampleIndex = 0;

            TimeSpan sampleSum = TimeSpan.Zero;
            TimeSpan comparisonSum = TimeSpan.Zero;

            for(int i = 1; i < sampleBeats; i++)
            {
                TimeSpan sampleBeatDuration = samples[i] - samples[i - 1];
                TimeSpan comparisonDuration = durations[sampleIndex];

                sampleSum += sampleBeatDuration;
                comparisonSum += comparisonDuration;

                if ((sampleSum - comparisonSum).Abs() > TimeSpan.FromMilliseconds(200))
                    return false;

                sampleIndex = (sampleIndex + 1) % durations.Count;
            }

            return true;
        }

        private List<TimeSpan> CalculateDurations(bool[] pattern, TimeSpan beatDuration)
        {
            List<TimeSpan> durations = new List<TimeSpan>();
            int len = 0;
            foreach (bool b in pattern)
            {
                if (b && len > 0)
                {
                    durations.Add(beatDuration.Multiply(len).Divide(pattern.Length));
                    len = 1;
                }
                else
                {
                    len++;
                }
            }

            if (len > 0)
            {
                durations.Add(beatDuration.Multiply(len).Divide(pattern.Length));
            }

            return durations;
        }

        private void btnRemoveSegment_Click(object sender, RoutedEventArgs e)
        {
            long marker = Position.Ticks;
            BeatSegment segment =
                Segments.FirstOrDefault(s => s.Position <= marker && s.Position + s.Duration >= marker);

            if (segment != null)
                Segments.Remove(segment);
        }
    }
}
