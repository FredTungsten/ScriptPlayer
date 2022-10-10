namespace ScriptPlayer.Shared
{
    public class ExtractSubtitleArguments : FfmpegArguments
    {
        public int StreamIndex { get; set; }
        public string OutputFile { get; set; }
        public string Format { get; set; }

        public override string BuildArguments()
        {
            return $"-i \"{InputFile}\" -map 0:{StreamIndex} -c copy -f {Format} \"{OutputFile}\"";
        }
    }
}