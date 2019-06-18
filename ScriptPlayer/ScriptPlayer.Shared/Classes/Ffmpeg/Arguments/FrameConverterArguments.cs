using System;
using System.Globalization;

namespace ScriptPlayer.Shared
{
    public class FrameConverterArguments : FfmpegArguments
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public double Intervall { get; set; }

        public string OutputDirectory { get; set; }

        public FrameConverterArguments()
        {
            Width = 200;
            Height = -1;
            Intervall = 10;
        }

        public override string BuildArguments()
        {
            if (string.IsNullOrEmpty(OutputDirectory))
                throw new ArgumentException("OutputDirectory must be set!");

            string intervall = Intervall.ToString("f3", CultureInfo.InvariantCulture);

            return $"-i \"{InputFile}\" -vf \"scale={Width}:{Height}, fps=1/{intervall}\" \"{OutputDirectory}%05d.jpg\" -stats";
        }
    }
}