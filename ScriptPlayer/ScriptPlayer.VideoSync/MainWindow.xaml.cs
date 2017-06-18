using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Newtonsoft.Json;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Scripts;
using ScriptPlayer.VideoSync.Dialogs;
using Brush = System.Windows.Media.Brush;

namespace ScriptPlayer.VideoSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty BeatBarDurationProperty = DependencyProperty.Register(
            "BeatBarDuration", typeof(double), typeof(MainWindow), new PropertyMetadata(5.0));

        public double BeatBarDuration
        {
            get { return (double) GetValue(BeatBarDurationProperty); }
            set { SetValue(BeatBarDurationProperty, value); }
        }

        public static readonly DependencyProperty BeatBarCenterProperty = DependencyProperty.Register(
            "BeatBarCenter", typeof(double), typeof(MainWindow), new PropertyMetadata(0.5));

        public double BeatBarCenter
        {
            get { return (double) GetValue(BeatBarCenterProperty); }
            set { SetValue(BeatBarCenterProperty, value); }
        }

        public static readonly DependencyProperty SampleXProperty = DependencyProperty.Register(
            "SampleX", typeof(int), typeof(MainWindow), new PropertyMetadata(680, OnSampleSizeChanged));

        private static void OnSampleSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).UpdateSampleSize();
        }

        private void UpdateSampleSize()
        {
            var rect = new Int32Rect(SampleX, SampleY, SampleW, SampleH);
            if (rect == _captureRect) return;

            SetCaptureRect(rect);
        }


        public int SampleX
        {
            get { return (int) GetValue(SampleXProperty); }
            set { SetValue(SampleXProperty, value); }
        }

        public static readonly DependencyProperty SampleYProperty = DependencyProperty.Register(
            "SampleY", typeof(int), typeof(MainWindow), new PropertyMetadata(700, OnSampleSizeChanged));

        public int SampleY
        {
            get { return (int) GetValue(SampleYProperty); }
            set { SetValue(SampleYProperty, value); }
        }

        public static readonly DependencyProperty SampleWProperty = DependencyProperty.Register(
            "SampleW", typeof(int), typeof(MainWindow), new PropertyMetadata(4, OnSampleSizeChanged));

        public int SampleW
        {
            get { return (int) GetValue(SampleWProperty); }
            set { SetValue(SampleWProperty, value); }
        }

        public static readonly DependencyProperty SampleHProperty = DependencyProperty.Register(
            "SampleH", typeof(int), typeof(MainWindow), new PropertyMetadata(4, OnSampleSizeChanged));
   
        public int SampleH
        {
            get { return (int) GetValue(SampleHProperty); }
            set { SetValue(SampleHProperty, value); }
        }

        public static readonly DependencyProperty BookmarksProperty = DependencyProperty.Register(
            "Bookmarks", typeof(List<TimeSpan>), typeof(MainWindow), new PropertyMetadata(default(List<TimeSpan>)));

        public List<TimeSpan> Bookmarks
        {
            get { return (List<TimeSpan>) GetValue(BookmarksProperty); }
            set { SetValue(BookmarksProperty, value); }
        }

        public static readonly DependencyProperty SamplerProperty = DependencyProperty.Register(
            "Sampler", typeof(ColorSampler), typeof(MainWindow), new PropertyMetadata(default(ColorSampler)));

        public ColorSampler Sampler
        {
            get { return (ColorSampler) GetValue(SamplerProperty); }
            set { SetValue(SamplerProperty, value); }
        }

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Duration", typeof(double), typeof(MainWindow), new PropertyMetadata(default(double)));

        public double Duration
        {
            get { return (double) GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        private RenderTargetBitmap _bitmap;
        private DrawingVisual _drawing;

        public static readonly DependencyProperty BeatsProperty = DependencyProperty.Register(
            "Beats", typeof(BeatCollection), typeof(MainWindow), new PropertyMetadata(default(BeatCollection)));

        public BeatCollection Beats
        {
            get { return (BeatCollection) GetValue(BeatsProperty); }
            set { SetValue(BeatsProperty, value); }
        }


        public static readonly DependencyProperty BeatCountProperty = DependencyProperty.Register(
            "BeatCount", typeof(int), typeof(MainWindow), new PropertyMetadata(default(int)));

        public int BeatCount
        {
            get { return (int) GetValue(BeatCountProperty); }
            set { SetValue(BeatCountProperty, value); }
        }


        public static readonly DependencyProperty PixelPreviewProperty = DependencyProperty.Register(
            "PixelPreview", typeof(Brush), typeof(MainWindow), new PropertyMetadata(default(Brush)));

        private bool _active;
        private bool _up;

        public Brush PixelPreview
        {
            get { return (Brush) GetValue(PixelPreviewProperty); }
            set { SetValue(PixelPreviewProperty, value); }
        }

        public MainWindow()
        {
            Bookmarks = new List<TimeSpan>();
            SetAllBeats(new BeatCollection()); 
            InitializeComponent();
        }

        private void InitializeSampler()
        {
            Sampler = new ColorSampler();
            Sampler.BeatDetected += SamplerOnBeatDetected;
            Sampler.Sample = _captureRect;

            RefreshSampler();
        }

        private void RefreshSampler()
        {
            Sampler.Resolution = videoPlayer.Resolution;
            Sampler.Source = videoPlayer.VideoBrush;
            Sampler.TimeSource = videoPlayer.TimeSource;
            Sampler.Refresh();
        }

        private void SamplerOnBeatDetected(object sender, TimeSpan d)
        {
            if (cckRecord.IsChecked != true) return;
            AddBeat(d);
            BeatCount = Beats.Count;
        }

        private Int32Rect _captureRect = new Int32Rect(680, 700, 4, 4);
        private bool _wasplaying;
        private string _openFile;

        private FrameCaptureCollection _frameSamples;
        private BeatCollection _originalBeats;
        private PixelColorSampleCondition _condition = new PixelColorSampleCondition();

        private TimeSpan _stretchFromBegin;
        private TimeSpan _stretchFromEnd;
        private TimeSpan _stretchToEnd;
        private TimeSpan _stretchToBegin;
        private TimeSpan _marker1;
        private TimeSpan _marker2;

        private void AddBeat(TimeSpan positionTotalSeconds)
        {
            Debug.WriteLine(positionTotalSeconds);
            Beats.Add(positionTotalSeconds);
        }

        private void mnuOpenVideo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog {Filter = "Videos|*.mp4;*.mpg;*.mpeg;*.avi|All Files|*.*"};
            if (dialog.ShowDialog(this) != true) return;

            OpenVideo(dialog.FileName);
        }

        private void SeekBar_OnSeek(object sender, double relative, TimeSpan absolute, int downMoveUp)
        {
            switch(downMoveUp)
            {
                case 0:
                    _wasplaying = videoPlayer.IsPlaying;
                    videoPlayer.Pause();
                    videoPlayer.SetPosition(absolute);
                    break;
                case 1:
                    videoPlayer.SetPosition(absolute);
                    break;
                case 2:
                    videoPlayer.SetPosition(absolute);
                    if(_wasplaying)
                        videoPlayer.Play();
                    break;
            }
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            videoPlayer.Pause();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            videoPlayer.Play();
        }

        private void mnuClear_Click(object sender, RoutedEventArgs e)
        {
            Beats.Clear();
        }

        private void mnuSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                FileName = Path.GetFileNameWithoutExtension(_openFile)
            };
            dialog.Filter = "Text-File|*.txt";
            if (dialog.ShowDialog(this) != true) return;

            Beats.Save(dialog.FileName);
        }

        private void mnuLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Text-File|*.txt";
            if (dialog.ShowDialog(this) != true) return;
            LoadBeatsFile(dialog.FileName);
        }

        private void LoadBeatsFile(string filename)
        {
            SetAllBeats(BeatCollection.Load(filename));
        }

        private void mnuQuickLoad_CLick(object sender, RoutedEventArgs e)
        {
            var videoFile = @"D:\Videos\CH\FH\Fap Hero - Pendulum (No-host).mp4";
            OpenVideo(videoFile);
        }

        private void videoPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeSampler();
        }

        private void VideoPlayer_OnMediaOpened(object sender, EventArgs e)
        {
            RefreshSampler();
        }

        private void videoPlayer_VideoMouseDown(object sender, int x, int y)
        {
            SetCaptureRect(new Int32Rect(x, y, SampleW, SampleH));
        }

        private void SetCaptureRect(Int32Rect rect)
        {
            _captureRect = rect;

            SampleX = rect.X;
            SampleY = rect.Y;
            SampleW = rect.Width;
            SampleH = rect.Height;

            Debug.WriteLine($"Set Sample Area to: {_captureRect.X} / {_captureRect.Y} ({_captureRect.Width} x {_captureRect.Height})");
            Sampler.Sample = _captureRect;
            videoPlayer.SampleRect = new Rect(_captureRect.X, _captureRect.Y, _captureRect.Width, _captureRect.Height);
        }

        private void VideoPlayer_OnVideoMouseUp(object sender, int x, int y)
        {
            //throw new NotImplementedException();
        }

        private void mnuFrameSampler_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FrameSamplerDialog(_openFile, _captureRect);
            if (dialog.ShowDialog() != true) return;

            _frameSamples = dialog.Result;
        }

        private void mnuSaveSamples_Click(object sender, RoutedEventArgs e)
        {
            if (_frameSamples == null)
            {
                MessageBox.Show("No Samples loaded!");
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "Frame Sample Files|*.framesamples|All Files|*.*",
                FileName = Path.GetFileNameWithoutExtension(_openFile)
            };

            if (dialog.ShowDialog(this) != true) return;

            _frameSamples.SaveToFile(dialog.FileName);
        }

        private void mnuLoadSamples_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { Filter = "Frame Sample Files|*.framesamples|All Files|*.*" };

            if (dialog.ShowDialog(this) != true) return;

            _frameSamples = FrameCaptureCollection.FromFile(dialog.FileName);

            if (_openFile != _frameSamples.VideoFile)
            {
                if (MessageBox.Show(this, "Load '" + _frameSamples.VideoFile + "'?", "Open associated file?",
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    OpenVideo(_frameSamples.VideoFile);
                }
            }

            SetCaptureRect(_frameSamples.CaptureRect);
        }

        private void OpenVideo(string videoFile, bool play = false)
        {
            _openFile = videoFile;
            videoPlayer.Open(videoFile);
            if(play)
                videoPlayer.Play();
        }

        private void mnuAnalyseSamples_Click(object sender, RoutedEventArgs e)
        {
            if (_frameSamples == null)
            {
                MessageBox.Show(this, "No samples to analyse!");
            }

            AnalysisParameters parameters = new AnalysisParameters();
            FrameAnalyserDialog dialog = new FrameAnalyserDialog(_frameSamples, _condition, parameters);

            if (dialog.ShowDialog() != true) return;

            SetAllBeats(dialog.Result);
        }

        private void SetAllBeats(IEnumerable<TimeSpan> beats)
        {
            _originalBeats = new BeatCollection(beats);
            Beats = _originalBeats.Duplicate();
        }

        private double GetDouble(double defaultValue)
        {
            DoubleInputDialog dialog = new DoubleInputDialog(defaultValue);
            dialog.Owner = this;
            if (dialog.ShowDialog() != true)
                return double.NaN;

            return dialog.Result;
        }

        private void mnuScale_Click(object sender, RoutedEventArgs e)
        {
            if (_originalBeats == null) return;
            double scale = GetDouble(1);
            if (double.IsNaN(scale)) return;

            Beats = Beats.Scale(scale);
        }

        private void mnuShift_Click(object sender, RoutedEventArgs e)
        {
            if (_originalBeats == null) return;
            double shift = GetDouble(0);
            if (double.IsNaN(shift)) return;

            Beats = Beats.Shift(TimeSpan.FromSeconds(shift));
        }

        private void mnuReset_Click(object sender, RoutedEventArgs e)
        {
            if (_originalBeats == null) return;
            Beats = _originalBeats.Duplicate();
        }

        private void btnAddBookmark_Click(object sender, RoutedEventArgs e)
        {
            List<TimeSpan> newBookMarks = new List<TimeSpan>(Bookmarks);
            newBookMarks.Add(videoPlayer.GetPosition());
            newBookMarks.Sort();

            Bookmarks = newBookMarks;
        }

        private void btnResetBookmarks_Click(object sender, RoutedEventArgs e)
        {
            Bookmarks = new List<TimeSpan>();
        }

        private void Bookmark_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = sender as ListBoxItem;
            if (item == null) return;

            if (!(item.DataContext is TimeSpan)) return;

            TimeSpan position = (TimeSpan) item.DataContext;

            videoPlayer.SetPosition(position);
        }

        private void mnuSetCondition_Click(object sender, RoutedEventArgs e)
        {
            ConditionEditorDialog dialog = new ConditionEditorDialog(_condition);
            if (dialog.ShowDialog() != true) return;

            SetCondition(dialog.Result);
        }

        private void SetCondition(PixelColorSampleCondition condition)
        {
            _condition = condition;
            colorSampleBar.SampleCondition = condition;
        }

        private void ShiftTime(TimeSpan timespan)
        {
            videoPlayer.SetPosition(videoPlayer.GetPosition() + timespan);
        }

        private void btnPreviousBookmark_Click(object sender, RoutedEventArgs e)
        {
            ShiftTime(TimeSpan.FromMinutes(-1));
        }

        private void btnSecondBack_Click(object sender, RoutedEventArgs e)
        {
            ShiftTime(TimeSpan.FromSeconds(-1));
        }

        private void btnFrameBack_Click(object sender, RoutedEventArgs e)
        {
            ShiftTime(TimeSpan.FromMilliseconds(-10));
        }

        private void btnFrameForward_Click(object sender, RoutedEventArgs e)
        {
            ShiftTime(TimeSpan.FromMilliseconds(10));
        }

        private void btnSecondForward_Click(object sender, RoutedEventArgs e)
        {
            ShiftTime(TimeSpan.FromSeconds(1));
        }

        private void btnNextBookmark_Click(object sender, RoutedEventArgs e)
        {
            ShiftTime(TimeSpan.FromMinutes(1));
        }
        private void btnMarker1_Click(object sender, RoutedEventArgs e)
        {
            _marker1 = videoPlayer.GetPosition();
        }

        private void btnMarker2_Click(object sender, RoutedEventArgs e)
        {
            _marker2 = videoPlayer.GetPosition();
        }

        private void btnStretchFromBegin_Click(object sender, RoutedEventArgs e)
        {
            _stretchFromBegin = videoPlayer.GetPosition();
        }

        private void btnStretchFromEnd_Click(object sender, RoutedEventArgs e)
        {
            _stretchFromEnd = videoPlayer.GetPosition();
        }

        private void btnStretchToEnd_Click(object sender, RoutedEventArgs e)
        {
            _stretchToEnd = videoPlayer.GetPosition();
        }

        private void btnStretchToBegin_Click(object sender, RoutedEventArgs e)
        {
            _stretchToBegin = videoPlayer.GetPosition();
        }

        private void btnStretchExecute_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan durationFrom = _stretchFromEnd - _stretchFromBegin;
            TimeSpan durationTo = _stretchToEnd - _stretchToBegin;

            if (durationTo <= TimeSpan.Zero || durationFrom <= TimeSpan.Zero)
                return;

            double factor = durationTo.Divide(durationFrom);
            TimeSpan shift = _stretchToBegin - _stretchFromBegin.Multiply(factor);

            //TimeSpan newBegin = _stretchFromBegin.Multiply(factor) + shift;
            //TimeSpan newEnd = _stretchFromEnd.Multiply(factor) + shift;

            var newbeats = _originalBeats.Scale(factor).Shift(shift);
            Beats = new BeatCollection(newbeats);
        }

        private void mnuJumpToFirstBeat_Click(object sender, RoutedEventArgs e)
        {
            if (Beats == null) return;
            if (Beats.Count == 0) return;
            TimeSpan beat = Beats.First();
            videoPlayer.SetPosition(beat);
        }

        private void mnuFindShortestBeat_Click(object sender, RoutedEventArgs e)
        {
            if (Beats == null) return;
            if (Beats.Count < 2) return;

            TimeSpan shortest = TimeSpan.MaxValue;
            TimeSpan position = TimeSpan.Zero;

            for (int i = 0; i < Beats.Count - 1; i++)
            {
                TimeSpan duration = Beats[i + 1] - Beats[i];
                if (duration < shortest)
                {
                    shortest = duration;
                    position = Beats[i];
                }
            }

            videoPlayer.SetPosition(position);
            MessageBox.Show("Shortest beat: " + shortest.TotalMilliseconds + " ms");
        }

        private void mnuJumpToLastBeat_Click(object sender, RoutedEventArgs e)
        {
            if (Beats == null) return;
            if (Beats.Count == 0) return;
            TimeSpan beat = Beats.Last();
            videoPlayer.SetPosition(beat);
        }

        private void mnuSaveKiiroo_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "Kiiroo JS|*.js|All Files|*.*",
                FileName = Path.GetFileNameWithoutExtension(_openFile)
            };

            if (dialog.ShowDialog(this) != true) return;

            using (StreamWriter writer = new StreamWriter(dialog.FileName, false))
            {
                writer.Write("var kiiroo_subtitles = {");

                CultureInfo culture = new CultureInfo("en-us");

                List<string> commands = new List<string>();

                bool up = false;
                foreach (TimeSpan timestamp in Beats)
                {
                    up ^= true;

                    commands.Add(String.Format(culture, "{0:f2}:{1}", timestamp.TotalSeconds,up?4:1));
                }

                writer.Write(String.Join(",", commands));

                writer.Write("};");
            }
        }

        private void mnuLoadFun_Click(object sender, RoutedEventArgs e)
        {
            //OpenFileDialog dialog = new OpenFileDialog();
            //dialog.Filter = "Funscript|*.funscript";

            //if (dialog.ShowDialog(this) != true)
            //    return;

            //FunScriptLoader loader = new FunScriptLoader();
            //loader.Load(dialog.FileName);

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Raw Script|*.raw";

            if (dialog.ShowDialog(this) != true)
                return;

            RawScriptLoader loader = new RawScriptLoader();
            loader.Load(dialog.FileName);
        }

        private void mnuSaveFunscript_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "Funscript|*.funscript|All Files|*.*",
                FileName = Path.GetFileNameWithoutExtension(_openFile)
            };

            if (dialog.ShowDialog(this) != true) return;

            FunScriptFile script = new FunScriptFile();
            script.Inverted = false;
            script.Range = 85;
            script.Version = new Version(1,0);

            bool up = false;
            foreach (TimeSpan timestamp in Beats)
            {
                up ^= true;

                script.Actions.Add(new FunScriptAction
                {
                    Position = (byte) (up?99:15),
                    TimeStamp = timestamp
                });
            }

            string content = JsonConvert.SerializeObject(script);

            File.WriteAllText(dialog.FileName, content, Encoding.UTF8);
        }

        private void btnResetSamples_Click(object sender, RoutedEventArgs e)
        {
            if (_frameSamples == null) return;

            SetCaptureRect(_frameSamples.CaptureRect);
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan tBegin = _marker1 < _marker2 ? _marker1 : _marker2;
            TimeSpan tEnd = _marker1 < _marker2 ? _marker2 : _marker1;

            SetAllBeats(Beats.Where(t => t < tBegin || t > tEnd));
        }

        private void mnuTrackBlob_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new VisualTrackerDialog(_openFile, _marker1, _marker2,new Rectangle(SampleX,SampleY,SampleW,SampleH));
            dialog.Show();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            Beats.Add(videoPlayer.GetPosition());
            SetAllBeats(Beats);
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            new MocktestDialog().Show();
        }

        private void mnuSaveProject_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(_openFile))
                return;

            SaveFileDialog dialog = new SaveFileDialog{Filter = "Beat Projects|*.bproj"};
            dialog.FileName = Path.ChangeExtension(_openFile, ".bproj");

            if (dialog.ShowDialog(this) != true)
                return;

            BeatProject project = new BeatProject
            {
                VideoFile = _openFile,
                SampleCondition = _condition,
                BeatBarDuration = BeatBar.TotalDisplayedDuration,
                BeatBarMidpoint = BeatBar.Midpoint,
                Beats = Beats.Select(b => b.Ticks).ToList(),
                Bookmarks = Bookmarks.Select(b => b.Ticks).ToList()
            };
            project.Save(dialog.FileName);
        }

        private void mnuLoadProject_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { Filter = "Beat Projects|*.bproj" };
            if (dialog.ShowDialog(this) != true)
                return;

            BeatProject project = BeatProject.Load(dialog.FileName);
            OpenVideo(project.VideoFile);
            SetCondition(project.SampleCondition);
            BeatBarDuration = project.BeatBarDuration;
            BeatBarCenter = project.BeatBarMidpoint;
            Beats = new BeatCollection(project.Beats.Select(TimeSpan.FromTicks));
            Bookmarks = project.Bookmarks.Select(TimeSpan.FromTicks).ToList();
        }
    }
}
