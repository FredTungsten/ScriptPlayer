using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScriptPlayer.Generators
{
    public class PreviewGeneratorSettings : FfmpegGeneratorSettings
    {
        public int Height { get; set; }
        public int Width { get; set; }
        public double Framerate { get; set; }
        public bool OverlayScriptPositions { get; set; }

        public List<TimeFrame> TimeFrames { get; set; }

        public PreviewGeneratorSettings()
        {
            TimeFrames = new List<TimeFrame>();

            Height = 170;
            Width = -2;
            Framerate = 24;

            OverlayScriptPositions = false;
            SkipIfExists = true;
        }

        public string SuggestDestination()
        {
            return Path.ChangeExtension(VideoFile, "gif");
        }

        public void GenerateRelativeTimeFrames(int sectionCount, TimeSpan durationEach)
        {
            TimeFrames.Clear();

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

        public PreviewGeneratorSettings Duplicate()
        {
            return new PreviewGeneratorSettings
            {
                Framerate = Framerate,
                Height = Height,
                Width = Width,
                SkipIfExists = SkipIfExists,
                ClipLeft = ClipLeft,
                TimeFrames = TimeFrames.Select(t => t.Duplicate()).ToList(),
                OverlayScriptPositions = OverlayScriptPositions
            };
        }

        public override bool IsIdenticalTo(FfmpegGeneratorSettings settings)
        {
            if (!(settings is PreviewGeneratorSettings previewSettings))
                return false;

            if (!base.IsIdenticalTo(settings))
                return false;

            if (previewSettings.Width != Width) return false;
            if (previewSettings.Height != Height) return false;
            if (Math.Abs(previewSettings.Framerate - Framerate) > 0.01) return false;
            if (OverlayScriptPositions != previewSettings.OverlayScriptPositions) return false;

            return true;
        }
    }
}