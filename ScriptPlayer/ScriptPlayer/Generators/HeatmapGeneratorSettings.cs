namespace ScriptPlayer.Generators
{
    public class HeatmapGeneratorSettings : FfmpegGeneratorSettings
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public bool AddShadow { get; set; }

        public HeatmapGeneratorSettings Duplicate()
        {
            return new HeatmapGeneratorSettings
            {
                AddShadow = AddShadow,
                Height = Height,
                SkipIfExists = SkipIfExists,
                Width = Width
            };
        }

        public HeatmapGeneratorSettings()
        {
            Width = 400;
            Height = 20;
            AddShadow = true;
            SkipIfExists = false;
        }
    }
}