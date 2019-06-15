namespace ScriptPlayer.Generators
{
    public class ThumbnailGeneratorSettings : FfmpegGeneratorSettings
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Intervall { get; set; }

        public ThumbnailGeneratorSettings DuplicateWithoutVideo()
        {
            return new ThumbnailGeneratorSettings
            {
                Width =  Width,
                Height = Height,
                Intervall = Intervall,
                SkipIfExists = SkipIfExists
            };
        }
    }
}