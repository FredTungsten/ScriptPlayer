using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
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
        private string _filePath;
        private TimeSpan _start;
        private ManualTimeSource _timeSource;

        public CreatePreviewDialog(string videoFilePath, TimeSpan start)
        {
            _timeSource = new ManualTimeSource(new DispatcherClock(Dispatcher, TimeSpan.FromMilliseconds(10)));
            _filePath = videoFilePath;
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

                string clip = Path.Combine(Path.GetTempPath(), Path.GetFileName(_filePath) + "-clip.mkv");
                string palette = Path.Combine(Path.GetTempPath(), Path.GetFileName(_filePath) + "-palette.png");
                string gif = Path.Combine(Path.GetDirectoryName(_filePath), Path.GetFileNameWithoutExtension(_filePath) + ".gif");

                string clipArguments = 
                    "-y " + //Yes to override existing files
                    $"-ss {startPosition:hh\\:mm\\:ss\\.ff} " + // Starting Position
                    $"-i \"{_filePath}\" " + // Input File
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

                string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D"));
                if (!tempPath.EndsWith("\\"))
                    tempPath += "\\";

                Directory.CreateDirectory(tempPath);

                string thumbArguments = $"-i \"{_filePath}\" -vf \"scale=200:-1, fps=1/10\" \"{tempPath}%04d.jpg\" -stats";

                SetStatus("Generating Thumbnails", 0);

                FrameConverterWrapper wrapper = new FrameConverterWrapper(ffmpegexe, thumbArguments);
                wrapper.ProgressChanged += (sender, progress) =>
                {
                    SetStatus("Generating Thumbnails", progress);
                };

                wrapper.Execute();

                SetStatus("Saving Thumbnails");

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

                string thumbfile = Path.ChangeExtension(_filePath, "thumbs");
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

            /*
             SET "INPUT_FILE=D:\Videos\CH\7. Done\CH\Awesome-X Cock Hero Temptation 4.mp4"
             SET "START_POSITION=3:14"
             SET "DURATION_SECONDS=5"
             SET "OUTPUT_HEIGHT=170"
             SET "OUTPUT_TYPE=gif"

             SET "FFMPEG=C:\Program Files (x86)\FFmpeg\bin\ffmpeg.exe"
             SET "CLIP_FILE=%INPUT_FILE%-clip.mkv"
             SET "PALETTE_FILE=%CLIP_FILE%-palette.png"

             "%FFMPEG%" -ss %START_POSITION% -i "%INPUT_FILE%" -t %DURATION_SECONDS% -vf select="mod(n-1\,2)","setpts=PTS-STARTPTS, hqdn3d=10, scale=-2:%OUTPUT_HEIGHT%" -vcodec libx264 -crf 24 "%CLIP_FILE%"
             
             "%FFMPEG%" -i "%CLIP_FILE%" -vf palettegen "%PALETTE_FILE%"

             "%FFMPEG%" -i "%CLIP_FILE%" -i "%PALETTE_FILE%" -filter_complex paletteuse -plays 0 "%INPUT_FILE%.%OUTPUT_TYPE%"

             pause
             */
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
    }

    public class ConsoleWrapper
    {
        private readonly ProcessStartInfo _startInfo;

        public ConsoleWrapper(string file, string arguments)
        {
            _startInfo = new ProcessStartInfo(file, arguments);
            _startInfo.UseShellExecute = false;
            _startInfo.CreateNoWindow = true;
            _startInfo.RedirectStandardError = true;
            _startInfo.RedirectStandardInput = true;
            _startInfo.RedirectStandardOutput = true;
        }

        public void Execute()
        {
            Process process = Process.Start(_startInfo);

            Thread _outputThread = new Thread(ReadOutput);
            _outputThread.Start(process.StandardOutput);

            Thread _errorThread = new Thread(ReadOutput);
            _errorThread.Start(process.StandardError);

            process.WaitForExit();

            _outputThread.Join();
            _errorThread.Join();
        }

        private void ReadOutput(object streamReader)
        {
            StreamReader reader = (StreamReader) streamReader;

            while (!reader.EndOfStream)
            {
                ProcessLine(reader.ReadLine());
            }
        }

        protected virtual void ProcessLine(string line)
        {
            Debug.WriteLine(line);
        }
    }

    public class FrameConverterWrapper : ConsoleWrapper
    {
        public event EventHandler<double> ProgressChanged;

        public FrameConverterWrapper(string file, string arguments) : base(file, arguments)
        {
        }

        //  Duration: 00:01:38.26
        Regex _durationRegex = new Regex(@"^\s*Duration:\s*(?<Duration>\d{2}:\d{2}:\d{2}\.\d{2})", RegexOptions.Compiled);

        //frame=   10 fps=2.8 q=1.6 Lsize=N/A time=00:01:40.00 bitrate=N/A speed=28.2x
        Regex _frameRegex = new Regex(@"^\s*frame=.*time=(?<Duration>\d{2}:\d{2}:\d{2}\.\d{2})", RegexOptions.Compiled);

        TimeSpan _duration = TimeSpan.Zero;

        protected override void ProcessLine(string line)
        {
            if (_durationRegex.IsMatch(line))
            {
                string duraString = _durationRegex.Match(line).Groups["Duration"].Value;
                Debug.WriteLine("DURATION: " + duraString);

                _duration = TimeSpan.ParseExact(duraString, "hh\\:mm\\:ss\\.ff", CultureInfo.InvariantCulture);
            }
            else if (_frameRegex.IsMatch(line))
            {
                string duraString = _frameRegex.Match(line).Groups["Duration"].Value;
                //Debug.WriteLine("POSITION: " + duraString);
                var position = TimeSpan.ParseExact(duraString, "hh\\:mm\\:ss\\.ff", CultureInfo.InvariantCulture);
                var progress = position.TotalSeconds / _duration.TotalSeconds;
                Debug.WriteLine("Progress: " + progress.ToString("P1"));

                OnProgressChanged(progress);
            }
        }

        protected virtual void OnProgressChanged(double e)
        {
            ProgressChanged?.Invoke(this, e);
        }
    }
}
