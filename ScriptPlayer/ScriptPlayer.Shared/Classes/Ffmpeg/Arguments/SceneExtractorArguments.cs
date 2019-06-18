using System;
using System.Collections.Generic;
using System.Globalization;

namespace ScriptPlayer.Shared
{
    public class SceneExtractorArguments : FfmpegArguments
    {
        public string OutputDirectory { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public double SceneDifferenceFactor { get; set; }

        public List<SceneFrame> Result { get; set; }

        public SceneExtractorArguments()
        {
            Width = 200;
            Height = -1;
            SceneDifferenceFactor = 0.5;
            Result = new List<SceneFrame>();
        }

        public override string BuildArguments()
        {
            if(string.IsNullOrEmpty(OutputDirectory))
                throw new ArgumentException("OutputDirectory must be set!");

            string sceneFactor = SceneDifferenceFactor.ToString("F", CultureInfo.InvariantCulture);

            return $"-i \"{InputFile}\" -vf \"select=gt(scene\\, {sceneFactor}),showinfo,scale={Width}:{Height}\" -vsync vfr \"{OutputDirectory}%05d.jpg\" -stats";
        }
    }
}