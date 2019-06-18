namespace ScriptPlayer.Shared
{
    public class VideoInfoArguments : FfmpegArguments
    {
        public override string BuildArguments()
        {
            return $"-i \"{InputFile}\" -hide_banner";
        }
    }
}