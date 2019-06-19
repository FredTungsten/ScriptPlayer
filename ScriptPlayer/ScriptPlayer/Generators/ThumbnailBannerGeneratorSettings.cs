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
                TotalWidth = TotalWidth
            };
        }
    }
}