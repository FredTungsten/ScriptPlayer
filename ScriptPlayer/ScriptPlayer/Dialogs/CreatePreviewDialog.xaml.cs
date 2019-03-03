using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScriptPlayer.Shared;

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
            _timeSource = new ManualTimeSource(new DispatcherClock(this.Dispatcher, TimeSpan.FromMilliseconds(10)));
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
                TimeSpan startPosition = new TimeSpan(0, 3, 11);
                int durationInSeconds = 5;
                int outputHeight = 170;

                string clip = _filePath + "-clip.mkv";
                string palette = clip + "-palette.png";
                string gif = _filePath + ".gif";

                var startInfo = new ProcessStartInfo(ffmpegexe);

                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                //startInfo.RedirectStandardError = true;
                //startInfo.RedirectStandardInput = true;
                //startInfo.RedirectStandardOutput = true;

                startInfo.Arguments =
                    "-y " + //Yes to override existing files
                    $"-ss {startPosition:hh\\:mm\\:ss} " + // Starting Position
                    $"-i \"{_filePath}\" " + // Input File
                    $"-t {durationInSeconds} " + //Duration
                    "-vf select=\"mod(n-1\\,2)\",\"" + //Every 2nd Frame
                    "setpts=PTS-STARTPTS, " +
                    "hqdn3d=10, " +
                    $"scale = -2:{outputHeight}\" " +
                    "-vcodec libx264 -crf 24 " +
                    $"\"{clip}\"";

                string clipOutput = "";

                Process processClip = Process.Start(startInfo);

                processClip.WaitForExit();
                //string clipErrors = processClip.StandardError.ReadToEnd();
                int clipResult = processClip.ExitCode;

                startInfo.Arguments = $"-y -i \"{clip}\" -vf palettegen \"{palette}\"";
                Process processPalette = Process.Start(startInfo);
                processPalette.WaitForExit();
                //string paletteErrors = processClip.StandardError.ReadToEnd();
                int paletteResult = processClip.ExitCode;

                startInfo.Arguments =
                    $"-y -i \"{clip}\" -i \"{palette}\" -filter_complex paletteuse -plays 0 \"{gif}\"";
                Process processGif = Process.Start(startInfo);
                processGif.WaitForExit();
                //string gifErrors = processClip.StandardError.ReadToEnd();
                int gifResult = processClip.ExitCode;

                Dispatcher.BeginInvoke(new Action(() =>
                {

                    if (File.Exists(gif))
                        gifPlayer.Load(gif);
                    else
                    {
                        MessageBox.Show(this, "Nope, didn't work");
                    }
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
    }
}
