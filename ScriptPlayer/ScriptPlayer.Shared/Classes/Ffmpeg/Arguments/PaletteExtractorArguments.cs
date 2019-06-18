namespace ScriptPlayer.Shared
{
    public class PaletteExtractorArguments : FfmpegArguments
    {
        public string OutputFile { get; set; }

        public override string BuildArguments()
        {
            return $"-stats -y -i \"{InputFile}\" -vf palettegen \"{OutputFile}\"";
        }
    }
}