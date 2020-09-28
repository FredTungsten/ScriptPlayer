namespace ScriptPlayer.Generators
{
    public class ThumbnailBannerGeneratorSettings : FfmpegGeneratorSettings
    {
        public int Rows { get; set; }

        public int Columns { get; set; }

        public int TotalWidth { get; set; }


        public ThumbnailBannerGeneratorSettings()
        {
            Rows = 5;
            Columns = 4;
            TotalWidth = 1024;
        }

        public ThumbnailBannerGeneratorSettings Duplicate()
        {
            return new ThumbnailBannerGeneratorSettings
            {
                Rows = Rows,
                Columns = Columns,
                TotalWidth = TotalWidth,
                ClipLeft = ClipLeft,
                SkipIfExists = SkipIfExists
            };
        }

        public override bool IsIdenticalTo(FfmpegGeneratorSettings settings)
        {
            if (!(settings is ThumbnailBannerGeneratorSettings bannerSettings))
                return false;

            if (!base.IsIdenticalTo(settings))
                return false;

            if (bannerSettings.Rows != Rows) return false;
            if (bannerSettings.Columns != Columns) return false;
            if (bannerSettings.TotalWidth != TotalWidth) return false;

            return true;
        }
    }
}