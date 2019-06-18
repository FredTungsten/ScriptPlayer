using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ScriptPlayer.Shared
{
    public class SceneExtractorWrapper : FfmpegConsoleWrapper
    {
        public SceneExtractorWrapper(SceneExtractorArguments arguments, string ffmpegExe) : base(arguments, ffmpegExe)
        {
            _sceneArguments = arguments;
        }

        protected override void AfterExecute(int exitCode)
        {
            base.AfterExecute(exitCode);

            if (_sceneArguments.Result.Count > 0)
            {
                _sceneArguments.Result.Last().Duration = _duration - _sceneArguments.Result.Last().TimeStamp;
            }
        }

        //  Duration: 00:01:38.26
        readonly Regex _durationRegex = new Regex(@"^\s*Duration:\s*(?<Duration>\d{2}:\d{2}:\d{2}\.\d{2})", RegexOptions.Compiled);

        //frame=   10 fps=2.8 q=1.6 Lsize=N/A time=00:01:40.00 bitrate=N/A speed=28.2x
        readonly Regex _frameRegex = new Regex(@"^\s*\[Parsed_showinfo.*\sn:\s*(?<Frame>\d+)\s+.*pts_time:\s*(?<Time>\d+(\.\d+)?)", RegexOptions.Compiled);

        private TimeSpan _duration = TimeSpan.Zero;
        private readonly SceneExtractorArguments _sceneArguments;

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

                if (_sceneArguments.Result.Count > 0)
                {
                    _sceneArguments.Result.Last().Duration = position - _sceneArguments.Result.Last().TimeStamp;
                }

                _sceneArguments.Result.Add(new SceneFrame
                {
                    Index = index,
                    TimeStamp = position,
                });
                
                UpdateProgress(progress);
            }
        }
    }
}