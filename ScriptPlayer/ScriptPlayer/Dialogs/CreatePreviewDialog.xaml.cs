using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Classes;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for CreatePreviewDialog.xaml
    /// </summary>
    public partial class CreatePreviewDialog : Window
    {
        private readonly string _videoFile;
        private readonly TimeSpan _start;
        private readonly ManualTimeSource _timeSource;
        private FrameConverterWrapper _wrapper;

        public CreatePreviewDialog(string videoFilePath, TimeSpan start)
        {
            _timeSource = new ManualTimeSource(new DispatcherClock(Dispatcher, TimeSpan.FromMilliseconds(10)));
            _videoFile = videoFilePath;
            _start = start;

            InitializeComponent();

            _timeSource.ProgressChanged += TimeSourceOnProgressChanged;

            gifPlayer.FramesReady += (sender, args) =>
            {
                gifPlayer.Width = gifPlayer.Frames.Width;
                gifPlayer.Height = gifPlayer.Frames.Height;
                _timeSource.Play();
            };


        }

        private void TimeSourceOnProgressChanged(object o, TimeSpan timeSpan)
        {
            gifPlayer.Progress = timeSpan.Divide(gifPlayer.Frames.Duration);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GeneratePreviewGif();
        }

        private void GeneratePreviewGif()
        {
            new Thread(() =>
            {
                // https://ffmpeg.zeranoe.com/builds/

                string ffmpegexe = @"C:\Program Files (x86)\FFmpeg\bin\ffmpeg.exe";
                TimeSpan startPosition = _start;
                int durationInSeconds = 5;
                int outputHeight = 170;

                string clip = Path.Combine(Path.GetTempPath(), Path.GetFileName(_videoFile) + "-clip.mkv");
                string palette = Path.Combine(Path.GetTempPath(), Path.GetFileName(_videoFile) + "-palette.png");
                string gif = Path.Combine(Path.GetDirectoryName(_videoFile), Path.GetFileNameWithoutExtension(_videoFile) + ".gif");

                string clipArguments = 
                    "-y " + //Yes to override existing files
                    $"-ss {startPosition:hh\\:mm\\:ss\\.ff} " + // Starting Position
                    $"-i \"{_videoFile}\" " + // Input File
                    $"-t {durationInSeconds} " + //Duration
                    "-vf select=\"mod(n-1\\,2)\",\"" + //Every 2nd Frame
                    "setpts=PTS-STARTPTS, " +
                    "hqdn3d=10, " +
                    $"scale = -2:{outputHeight}\" " +
                    "-vcodec libx264 -crf 24 " +
                    $"\"{clip}\"";

                string paletteArguments = $"-stats -y -i \"{clip}\" -vf palettegen \"{palette}\"";
                string gifArguments = $"-stats -y -i \"{clip}\" -i \"{palette}\" -filter_complex paletteuse -plays 0 \"{gif}\"";

                SetStatus("Generating GIF (1/3): Clipping Video Section");

                ConsoleWrapper clipWrapper = new ConsoleWrapper(ffmpegexe, clipArguments);
                clipWrapper.Execute();

                SetStatus("Generating GIF (2/3): Extracting Palette");

                ConsoleWrapper paletteWrapper = new ConsoleWrapper(ffmpegexe, paletteArguments);
                paletteWrapper.Execute();

                SetStatus("Generating GIF (3/3): Creating GIF");

                ConsoleWrapper gifWrapper = new ConsoleWrapper(ffmpegexe, gifArguments);
                gifWrapper.Execute();

                if(File.Exists(clip))
                    File.Delete(clip);

                if(File.Exists(palette))
                    File.Delete(palette);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (File.Exists(gif))
                        gifPlayer.Load(gif);
                }));

                

                //string thumbArguments = $"-i \"{_filePath}\" -vf \"scale=200:-1, fps=1/10\" \"{tempPath}%04d.jpg\" -stats";

                SetStatus("Generating Thumbnails", 0);

                FrameConverterWrapper wrapper = new FrameConverterWrapper(ffmpegexe);
                wrapper.Intervall = 10; //1 Frame every 10 seconds;
                wrapper.VideoFile = _videoFile;
                wrapper.Width = 200;
                wrapper.ProgressChanged += (sender, progress) =>
                {
                    SetStatus("Generating Thumbnails", progress);
                };

                _wrapper = wrapper;

                wrapper.Execute();

                SetStatus("Saving Thumbnails");

                string tempPath = wrapper.OutputPath;

                VideoThumbnailCollection thumbnails = new VideoThumbnailCollection();

                List<string> usedFiles = new List<string>();

                foreach (string file in Directory.EnumerateFiles(tempPath))
                {
                    string number = Path.GetFileNameWithoutExtension(file);
                    int index = int.Parse(number);


                    TimeSpan position = TimeSpan.FromSeconds(index * 10 - 5);

                    var frame = new BitmapImage();
                    frame.BeginInit();
                    frame.CacheOption = BitmapCacheOption.OnLoad;
                    frame.UriSource = new Uri(file, UriKind.Absolute);
                    frame.EndInit();

                    thumbnails.Add(position, frame);
                    usedFiles.Add(file);
                }

                string thumbfile = Path.ChangeExtension(_videoFile, "thumbs");
                using (FileStream stream = new FileStream(thumbfile, FileMode.Create, FileAccess.Write))
                {
                    thumbnails.Save(stream);
                }

                thumbnails.Dispose();

                foreach(string tempFile in usedFiles)
                    File.Delete(tempFile);

                Directory.Delete(tempPath);
                
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SetStatus("Done!", 1);
                    DialogResult = true;
                    Close();
                }));

            }).Start();
        }

        private void SetStatus(string text, double progress = -1)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
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
            }));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _wrapper.Cancel();
        }
    }
}
