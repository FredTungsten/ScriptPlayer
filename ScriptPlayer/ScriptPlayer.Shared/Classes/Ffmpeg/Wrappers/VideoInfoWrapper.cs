using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ScriptPlayer.Shared
{
    public class VideoInfoWrapper : FfmpegConsoleWrapper
    {
        public VideoInfo Result { get; }

        public VideoInfoWrapper(VideoInfoArguments arguments, string ffmpegExe) : base(arguments, ffmpegExe)
        {
            Result = new VideoInfo();
        }

        // Duration: 00:02:17.59, start: 0.000000, bitrate: 11106 kb/s
        private readonly Regex _durationRegex = new Regex(@"^\s*Duration:\s*(?<Duration>\d{2}:\d{2}:\d{2}\.\d{2})", RegexOptions.Compiled);

        // Stream #0:0(eng): Audio: aac (LC) (mp4a / 0x6134706D), 48000 Hz, stereo, fltp, 127 kb/s (default)
        private readonly Regex _audioRegex = new Regex(@"^(?<Codec>[^,]*?)(\(.*?\))?, (?<SampleRate>[^,]*?) Hz,", RegexOptions.Compiled);

        // Stream #0:1(eng): Video: h264 (High) (avc1 / 0x31637661), yuv420p, 1920x1080 [SAR 1:1 DAR 16:9], 10971 kb/s, 59.94 fps, 59.94 tbr, 60k tbn, 119.88 tbc (default)
        private readonly Regex _videoRegex = new Regex(@"^(?<Codec>[^,]*?)(\(.*?\))*,([^,]*?,\s*)*(?<Resolution>\d+x\d+)([^,]*?, \s*)*((?<FrameRate>[^,]*?) fps)?", RegexOptions.Compiled);

        private readonly Regex _detailRegex = new Regex(@"((\s*)(?<content>[^,\(\)]*)(\s*\([^\(\)]*\)\s*)*,?)*", RegexOptions.Compiled);

        private readonly Regex _resolutionRegex = new Regex(@"(?<Width>\d+)x(?<Height>\d+)", RegexOptions.Compiled);

        private readonly Regex _sampleRateRegex = new Regex(@"(?<SampleRate>.*?) Hz", RegexOptions.Compiled);

        private readonly Regex _frameRateRegex = new Regex(@"(?<FrameRate>.*?) fps", RegexOptions.Compiled);

        private readonly Regex _streamRegex = new Regex(@"^\s*Stream #(?<Input>\d+):(?<StreamId>\d+)(\[(?<HexInfo>.*?)\])?(\((?<Language>.*?)\))?:\s*(?<Type>.*?):\s*(?<Details>.*)\s*$", RegexOptions.Compiled);

        protected override void ProcessLine(string line, bool isError)
        {
            base.ProcessLine(line, isError);

            Match durationMatch = _durationRegex.Match(line);
            if (durationMatch.Success)
            {
                string duraString = _durationRegex.Match(line).Groups["Duration"].Value;
                Debug.WriteLine("DURATION: " + duraString);

                Result.Duration = TimeSpan.ParseExact(duraString, "hh\\:mm\\:ss\\.ff", CultureInfo.InvariantCulture);
                return;
            }

            Match streamRegex = _streamRegex.Match(line);
            if (streamRegex.Success)
            {
                string type = streamRegex.Groups["Type"].Value;
                string details = streamRegex.Groups["Details"].Value;

                Match detailMatches = _detailRegex.Match(details);
                if (!detailMatches.Success)
                    return;

                switch (type)
                {
                    case "Data":
                    {
                        break;
                    }
                    case "Audio":
                    {
                        if (!string.IsNullOrEmpty(Result.AudioCodec))
                            return;

                        Result.AudioCodec = detailMatches.Groups["content"].Captures[0].Value;

                        foreach (Capture capture in detailMatches.Groups["content"].Captures)
                        {
                            Match sampleMatch = _sampleRateRegex.Match(capture.Value);
                            if(sampleMatch.Success)
                            {
                                if (double.TryParse(sampleMatch.Groups["SampleRate"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double sampleRate))
                                    Result.SampleRate = sampleRate;
                            }
                        }

                        break;
                    }
                    case "Video":
                    {
                        if (!string.IsNullOrEmpty(Result.VideoCodec))
                            return;

                        Result.VideoCodec = detailMatches.Groups["content"].Captures[0].Value;

                        foreach (Capture capture in detailMatches.Groups["content"].Captures)
                        {
                            Match resolutionMatch = _resolutionRegex.Match(capture.Value);
                            if (resolutionMatch.Success)
                            {
                                if (Resolution.TryParse(resolutionMatch.Value, out Resolution resolution))
                                {
                                    Result.Resolution = resolution;
                                    continue;
                                }
                            }

                            Match framerateMatch = _frameRateRegex.Match(capture.Value);
                            if (framerateMatch.Success)
                            {
                                if (double.TryParse(framerateMatch.Groups["FrameRate"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double frameRate))
                                    Result.FrameRate = frameRate;
                            }
                        }

                        break;
                    }
                    case "Subtitle":
                    {
                        // e.g. Subtitle '(eng)' found in Stream 2: ass (default)
                        //                                          subrip

                        const string defaultMarker = "(default)";
                        string format = detailMatches.Groups["content"].Captures[0].Value;
                        string language = streamRegex.Groups["Language"].Value;
                        string streamId = streamRegex.Groups["StreamId"].Value;
                        bool isDefault = format.Contains(defaultMarker);

                        if (format.Contains("("))
                            format = format.Substring(0,
                                format.IndexOf("(", StringComparison.CurrentCultureIgnoreCase));

                        Console.WriteLine($"Subtitle '{language}' found in Stream {streamId}: {format} (isDefault = {isDefault}");

                        Result.Subtitles.Add(new SubtitleStream
                        {
                            StreamId = int.Parse(streamId),
                            Language = language,
                            IsDefault = isDefault,
                            Format = format.Trim()
                        });

                        break;
                    }
                    case "Attachment":
                    {
                        break;
                    }
                    default:
                    {
                        break;
                    }
                }
            }
        }
    }

    public class SubtitleStream
    {
        public int StreamId { get; set; }
        public string Language { get; set; }
        public bool IsDefault { get; set; }
        public string Format { get; set; }
    }
}