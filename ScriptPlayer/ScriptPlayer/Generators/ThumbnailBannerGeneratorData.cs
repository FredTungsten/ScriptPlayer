using ScriptPlayer.Shared;

namespace ScriptPlayer.Generators
{
    public class ThumbnailBannerGeneratorData
    {
        public ThumbnailBannerGeneratorSettings Settings { get; set; }

        public ThumbnailBannerGeneratorImage[] Images { get; set; }

        public string VideoName { get; set; }

        public VideoInfo VideoInfo { get; set; }

        public long FileSize { get; set; }
    }
}