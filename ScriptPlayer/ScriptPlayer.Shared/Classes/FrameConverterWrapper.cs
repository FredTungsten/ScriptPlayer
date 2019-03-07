using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace ScriptPlayer.Shared
{
    public class FrameConverterWrapper : ConsoleWrapper
    {
        public string VideoFile { get; set; }

        public string OutputPath { get; protected set; }

        public int Width { get; set; }

        public int Intervall { get; set; }

        protected FrameConverterWrapper()
        {
            CreateOutputPath();
            Width = 200;
            Intervall = 10;
        }

        public FrameConverterWrapper(string ffmpegExe) : this()
        {
            File = ffmpegExe;
        }

        private void CreateOutputPath()
        {
            OutputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D"));
            if (!OutputPath.EndsWith("\\"))
                OutputPath += "\\";

            Directory.CreateDirectory(OutputPath);
        }

        protected override void BeforeExecute()
        {
            Arguments = $"-i \"{VideoFile}\" -vf \"scale={Width}:-1, fps=1/{Intervall}\" \"{OutputPath}%05d.jpg\" -stats";
        }

        public void Cancel()
        {
            Input("q");
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
    }
}