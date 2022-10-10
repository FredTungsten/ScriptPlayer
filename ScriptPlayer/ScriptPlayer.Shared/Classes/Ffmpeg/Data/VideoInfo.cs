using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ScriptPlayer.Shared
{
    public class VideoInfo
    {
        public TimeSpan Duration { get; set; }

        public string AudioCodec { get; set; }

        public double SampleRate { get; set; }

        public string VideoCodec { get; set; }

        public Resolution Resolution { get; set; }

        public double FrameRate { get; set; }

        public List<SubtitleStream> Subtitles { get; set; }

        public VideoInfo()
        {
            Duration = TimeSpan.Zero;
            Subtitles = new List<SubtitleStream>();
        }

        public void DumpInfo()
        {
            Debug.WriteLine("Duration:   " + Duration);
            Debug.WriteLine("AudioCodec: " + AudioCodec);
            Debug.WriteLine("SampleRate: " + SampleRate);
            Debug.WriteLine("VideoCodec: " + VideoCodec);
            Debug.WriteLine("Resolution: " + Resolution);
            Debug.WriteLine("FrameRate:  " + FrameRate);
        }

        public bool IsComplete()
        {
            return Duration > TimeSpan.Zero &&
                   !string.IsNullOrWhiteSpace(AudioCodec) &&
                   !string.IsNullOrWhiteSpace(VideoCodec) &&
                   SampleRate > 0 &&
                   Resolution.Horizontal > 0 &&
                   Resolution.Vertical > 0 &&
                   FrameRate > 0;
        }

        public bool IsGoodEnough()
        {
            return Duration > TimeSpan.Zero &&
                   !string.IsNullOrWhiteSpace(VideoCodec) &&
                   Resolution.Horizontal > 0 &&
                   Resolution.Vertical > 0;
        }
    }
}