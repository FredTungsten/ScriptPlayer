using Newtonsoft.Json;

namespace ScriptPlayer.HandyApi.Messages
{
    public class HsspPlayRequest
    {
          [JsonProperty("start_time")]
        public long StartTime { get; set; }

        [JsonProperty("server_time")]
        public long ServerTime { get; set; }

        [JsonProperty("playback_rate")]
        public double PlaybackRate { get; set; }

        [JsonProperty("loop")]
        public bool Loop { get; set; }
    }
}
