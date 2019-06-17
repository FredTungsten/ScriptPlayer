using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ScriptPlayer.Shared
{
    public class SceneExtractorWrapper : FfmpegWrapper
    {

        public string OutputPath { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public double SceneDifferenceFactor { get; set; }

        public List<SceneFrame> Result { get; private set; }

        public SceneExtractorWrapper(string ffmpegExe) : base(ffmpegExe)
        {
            Width = 200;
            Height = -1;
            SceneDifferenceFactor = 0.5;
        }

        private void CreateOutputPath()
        {
            OutputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D"));
            if (!OutputPath.EndsWith("\\"))
                OutputPath += "\\";

            Directory.CreateDirectory(OutputPath);
        }

        public void GenerateRandomOutputPath()
        {
            CreateOutputPath();
        }

        protected override void BeforeExecute()
        {
            if (string.IsNullOrEmpty(OutputPath))
                CreateOutputPath();

            Result = new List<SceneFrame>();

            base.BeforeExecute();
        }

        protected override void AfterExecute(int exitCode)
        {
            base.AfterExecute(exitCode);

            if (Result.Count > 0)
            {
                Result.Last().Duration = _duration - Result.Last().TimeStamp;
            }
        }

        public event EventHandler<double> ProgressChanged;

        //  Duration: 00:01:38.26
        readonly Regex _durationRegex = new Regex(@"^\s*Duration:\s*(?<Duration>\d{2}:\d{2}:\d{2}\.\d{2})", RegexOptions.Compiled);

        //frame=   10 fps=2.8 q=1.6 Lsize=N/A time=00:01:40.00 bitrate=N/A speed=28.2x
        readonly Regex _frameRegex = new Regex(@"^\s*\[Parsed_showinfo.*\sn:\s*(?<Frame>\d+)\s+.*pts_time:\s*(?<Time>\d+(\.\d+)?)", RegexOptions.Compiled);

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
                var match = _frameRegex.Match(line);
                string duraString = match.Groups["Time"].Value;
                string frameString = match.Groups["Frame"].Value;

                TimeSpan position = TimeSpan.FromSeconds(double.Parse(duraString, CultureInfo.InvariantCulture));
                int index = int.Parse(frameString) + 1;

                double progress = position.TotalSeconds / _duration.TotalSeconds;
                Debug.WriteLine($"Progress: {progress:P1} (Frame {index})");

                if (Result.Count > 0)
                {
                    Result.Last().Duration = position - Result.Last().TimeStamp;
                }

                Result.Add(new SceneFrame
                {
                    Index = index,
                    TimeStamp = position,
                });
                
                OnProgressChanged(progress);
            }
        }

        protected virtual void OnProgressChanged(double e)
        {
            ProgressChanged?.Invoke(this, e);
        }

        protected override void SetArguments()
        {
            string sceneFactor = SceneDifferenceFactor.ToString("F", CultureInfo.InvariantCulture);

            Arguments = $"-i \"{VideoFile}\" -vf \"select=gt(scene\\, {sceneFactor}),showinfo,scale={Width}:{Height}\" -vsync vfr \"{OutputPath}%05d.jpg\" -stats";
        }
    }
}