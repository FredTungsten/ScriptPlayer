using System;
using System.Collections.Generic;
using System.IO;

namespace ScriptPlayer.Generators
{
    public class PreviewGeneratorSettings : FfmpegGeneratorSettings
    {
        public int Height { get; set; }
        public int Width { get; set; }
        public int Framerate { get; set; }

        public List<TimeFrame> TimeFrames { get; set; }

        public PreviewGeneratorSettings()
        {
            TimeFrames = new List<TimeFrame>();

            Height = 170;
            Width = -2;
            Framerate = 24;

            SkipIfExists = true;
        }

        public string SuggestDestination()
        {
            return Path.ChangeExtension(VideoFile, "gif");
        }

        public void GenerateRelativeTimeFrames(int sectionCount, TimeSpan durationEach)
        {
            double spacing = 1.0 / (sectionCount + 1);

            for (int i = 0; i < sectionCount; i++)
            {
                TimeFrames.Add(new TimeFrame
                {
                    Duration = durationEach,
                    StartFactor = spacing * (i + 1)
                });
            }
        }
    }
}