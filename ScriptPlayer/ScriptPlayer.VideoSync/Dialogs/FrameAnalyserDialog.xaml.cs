using System;
using System.Threading;
using System.Windows;
using System.Windows.Shell;
using ScriptPlayer.Shared;

namespace ScriptPlayer.VideoSync
{
    /// <summary>
    /// Interaction logic for FrameAnalyserDialog.xaml
    /// </summary>
    public partial class FrameAnalyserDialog : Window
    {
        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
            "Result", typeof(VideoAnalysisResult), typeof(FrameAnalyserDialog), new PropertyMetadata(default(VideoAnalysisResult)));

        public VideoAnalysisResult Result
        {
            get { return (VideoAnalysisResult) GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        private readonly FrameCaptureCollection _frames;
        private readonly SampleCondition _condition;
        private readonly AnalysisParameters _parameters;

        private bool _running;
        private Thread _t;

        public FrameAnalyserDialog(FrameCaptureCollection frames, SampleCondition condition, AnalysisParameters parameters)
        {
            TaskbarItemInfo = new TaskbarItemInfo();
            _condition = condition;
            _parameters = parameters;
            _frames = frames;
            InitializeComponent();
        }

        public void Analyse()
        {
            VideoAnalysisResult result = new VideoAnalysisResult();
            result.TotalFrames = _frames.TotalFramesInVideo;
            int frameIndex = 0;

            SampleAnalyser analyser = new SampleAnalyser(_condition, _parameters);

            foreach (var frame in _frames)
            {
                if (!_running) break;

                if (analyser.AddSample(frame.Capture))
                {
                    result.Beats.Add(frameIndex);

                    //if (frame.FrameIndex != frameIndex)
                    //{
                    //    frameIndex = frameIndex;
                    //}
                }
                
                if ((frameIndex+1) % 100 == 0)
                {
                    double progress01 = (double) frameIndex / _frames.Count;
                    string progress = $"{frameIndex} / {_frames.Count} ({progress01:P}) - {result.Beats.Count} Beats";

                    Dispatcher.Invoke(() =>
                    {
                        TaskbarItemInfo.ProgressValue = progress01;
                        proProgress.Value = progress01;
                        txtProgress.Text = progress;
                    });
                }

                frameIndex++;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                Result = result;
                DialogResult = true;
            }));
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            _running = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _running = true;
            _t = new Thread(Analyse);
            _t.Start();
        }
    }
}
