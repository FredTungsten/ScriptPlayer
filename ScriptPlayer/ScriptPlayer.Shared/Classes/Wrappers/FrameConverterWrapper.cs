using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace ScriptPlayer.Shared
{
    public class FrameConverterWrapper : FfmpegWrapper
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public double Intervall { get; set; }

        public string OutputDirectory { get; set; }

        public FrameConverterWrapper(string ffmpegExe) : base(ffmpegExe)
        {
            Width = 200;
            Height = -1;
            Intervall = 10;
        }

        private void CreateOutputDirectory()
        {
            OutputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D"));
            if (!OutputDirectory.EndsWith("\\"))
                OutputDirectory += "\\";

            Directory.CreateDirectory(OutputDirectory);
        }

        public void GenerateRandomOutputPath()
        {
            CreateOutputDirectory();
        }

        protected override void BeforeExecute()
        {
            if(string.IsNullOrEmpty(OutputDirectory))
                CreateOutputDirectory();

            base.BeforeExecute();
        }

        public event EventHandler<double> ProgressChanged;

        //  Duration: 00:01:38.26
        readonly Regex _durationRegex = new Regex(@"^\s*Duration:\s*(?<Duration>\d{2}:\d{2}:\d{2}\.\d{2})", RegexOptions.Compiled);

        //frame=   10 fps=2.8 q=1.6 Lsize=N/A time=00:01:40.00 bitrate=N/A speed=28.2x
        readonly Regex _frameRegex = new Regex(@"^\s*frame=.*time=(?<Duration>\d{2}:\d{2}:\d{2}\.\d{2})", RegexOptions.Compiled);

        private TimeSpan _duration = TimeSpan.Zero;

        protected override void ProcessLine(string line, bool isError)
        {
            base.ProcessLine(line, isError);

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

        protected override void SetArguments()
        {
            string intervall = Intervall.ToString("f3", CultureInfo.InvariantCulture);

            Arguments = $"-i \"{VideoFile}\" -vf \"scale={Width}:{Height}, fps=1/{intervall}\" \"{OutputDirectory}%05d.jpg\" -stats";
        }
    }
}