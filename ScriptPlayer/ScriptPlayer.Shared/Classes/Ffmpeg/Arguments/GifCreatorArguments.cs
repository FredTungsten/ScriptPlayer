using System.Globalization;

namespace ScriptPlayer.Shared
{
    public class GifCreatorArguments : FfmpegArguments
    {
        public double Framerate { get; set; }

        public string PaletteFile { get; set; }

        public string OutputFile { get; set; }

        public override string BuildArguments()
        {
            string framerate = Framerate.ToString("F2", CultureInfo.InvariantCulture);
            return $"-stats -y -r {framerate} -i \"{InputFile}\" -i \"{PaletteFile}\" -filter_complex paletteuse -plays 0 \"{OutputFile}\"";
        }
    }
}