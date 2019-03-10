using System;
using System.IO;
using System.Threading;
using System.Windows;
using ScriptPlayer.Shared;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for CreatePreviewDialog.xaml
    /// </summary>
    public partial class CreatePreviewDialog : Window
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(MainViewModel), typeof(CreatePreviewDialog), new PropertyMetadata(default(MainViewModel)));

        public MainViewModel ViewModel
        {
            get { return (MainViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        private readonly PreviewGeneratorSettings _settings;

        private ConsoleWrapper _wrapper;
        private bool _done;
        private Thread _thread;
        private bool _success;
        private bool _canceled;
        
        public CreatePreviewDialog(MainViewModel viewModel, PreviewGeneratorSettings settings)
        {
            ViewModel = viewModel;
            _settings = settings;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GeneratePreviewGif();
        }

        private void GeneratePreviewGif()
        {
            string ffmpegexe = ViewModel.Settings.FfmpegPath;

            _thread = new Thread(() =>
            {
                string clip = Path.Combine(Path.GetTempPath(), Path.GetFileName(_settings.Video) + "-clip.mkv");
                string palette = Path.Combine(Path.GetTempPath(), Path.GetFileName(_settings.Video) + "-palette.png");
                string gif = _settings.Destination;
                int framerate = _settings.Framerate;

                try
                {
                    string clipArguments =
                        "-y " +                                                     //Yes to override existing files
                        $"-ss {_settings.Start:hh\\:mm\\:ss\\.ff} " +               // Starting Position
                        $"-i \"{_settings.Video}\" " +                              // Input File
                        $"-t {_settings.Duration:hh\\:mm\\:ss\\.ff} " +             // Duration
                        $"-r {framerate} " + 
                        "-vf " +                                                    // video filter parameters" +
                        //$"select=\"mod(n-1\\,{_settings.FramerateDivisor})\"," +    // Every 2nd Frame
                        $"\"setpts=PTS-STARTPTS, hqdn3d=10, scale = {_settings.Width}:{_settings.Height}\" " +
                        "-vcodec libx264 -crf 0 " +
                        $"\"{clip}\"";

                    string paletteArguments = $"-stats -y -i \"{clip}\" -vf palettegen \"{palette}\"";
                    string gifArguments = $"-stats -y -r {framerate} -i \"{clip}\" -i \"{palette}\" -filter_complex paletteuse -plays 0 \"{gif}\"";

                    SetStatus("Generating GIF (1/3): Clipping Video Section", 0);

                    _wrapper = new ConsoleWrapper(ffmpegexe);
                    _wrapper.Execute(clipArguments);

                    if (_canceled)
                        return;

                    SetStatus("Generating GIF (2/3): Extracting Palette", 1 / 3.0);

                    _wrapper.Execute(paletteArguments);

                    if (_canceled)
                        return;

                    SetStatus("Generating GIF (3/3): Creating GIF", 2 / 3.0);

                    _wrapper.Execute(gifArguments);

                    if (_canceled)
                        return;

                   SetStatus("Done!", 3 / 3.0);
                    _success = File.Exists(gif);
                    _done = true;

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        btnClose.Content = "Close";

                        if (_success)
                        {
                            SetStatus("Done!", 3 / 3.0);
                            gifPlayer.Load(gif);
                        }
                        else
                        {
                            SetStatus("Failed!", 3 / 3.0);
                        }
                    }));
                }
                finally
                {
                    if (File.Exists(clip))
                        File.Delete(clip);

                    if (File.Exists(palette))
                        File.Delete(palette);
                }
            });

            _thread.Start();
        }

        private void SetStatus(string text, double progress = -1)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SetStatus(text, progress);
                }));
                return;
            }

            if (progress < 0)
            {
                proConversion.IsIndeterminate = true;
            }
            else
            {
                proConversion.IsIndeterminate = false;
                proConversion.Value = Math.Min(1, Math.Max(0, progress));
            }

            txtStatus.Text = text;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_done)
            {
                DialogResult = _success;
            }
            else
            {
                _canceled = true;
                _wrapper?.Input("q");
                if(_thread.Join(TimeSpan.FromSeconds(5)))
                    _thread.Abort();
                DialogResult = false;
            }
        }
    }

    public class PreviewGeneratorSettings
    {
        public string Video { get; set; }
        public string Destination { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan Duration { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public int Framerate { get; set; }

        public PreviewGeneratorSettings()
        {
            Start = TimeSpan.Zero;
            Duration = TimeSpan.FromSeconds(5);
            Height = 170;
            Width = -2;
            Framerate = 24;
        }

        public string SuggestDestination()
        {
            return Path.ChangeExtension(Video, "gif");
        }
    }
}
