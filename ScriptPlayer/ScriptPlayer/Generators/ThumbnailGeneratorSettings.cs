namespace ScriptPlayer.Generators
{
    public class ThumbnailGeneratorSettings : FfmpegGeneratorSettings
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Intervall { get; set; }

        public ThumbnailGeneratorSettings()
        {
            Width = 200;
            Height = -1;
            Intervall = -1;

            SkipIfExists = true;
        }

        public ThumbnailGeneratorSettings Duplicate()
        {
            return new ThumbnailGeneratorSettings
            {
                Width =  Width,
                Height = Height,
                Intervall = Intervall,
                SkipIfExists = SkipIfExists,
                ClipLeft = ClipLeft
            };
        }

        public override bool IsIdenticalTo(FfmpegGeneratorSettings settings)
        {
            if (!(settings is ThumbnailGeneratorSettings thumbnailSettings))
                return false;

            if (!base.IsIdenticalTo(settings))
                return false;

            if (thumbnailSettings.Width != Width) return false;
            if (thumbnailSettings.Height != Height) return false;
            if (thumbnailSettings.Intervall != Intervall) return false;

            return true;
        }
    }
}