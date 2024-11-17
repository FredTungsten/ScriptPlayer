
using Newtonsoft.Json;

namespace ScriptPlayer.HandyAPIv3Playground.TheHandyV3.Messages.Hssp
{
    public class HsspStateResponse
    {
        [JsonProperty("play_state")]
        public int PlayState { get; set; }

        [JsonProperty("pause_on_starving")]
        public bool PauseOnStarving { get; set; }
        
        [JsonProperty("points")]
        public int Points { get; set; }
        
        [JsonProperty("max_points")]
        public int MaxPoints { get; set; }

        [JsonProperty("current_point")]
        public int CurrentPoint { get; set; }

        [JsonProperty("current_time")]
        public int CurrentTime { get; set; }

        [JsonProperty("loop")]
        public bool Loop { get; set; }

        [JsonProperty("playback_rate")]
        public double PlaybackRate { get; set; }

        [JsonProperty("first_point_time")]
        public int FirstPointTime { get; set; }

        [JsonProperty("last_point_time")]
        public int LastPointTime { get; set; }

        [JsonProperty("stream_id")]
        public int StreamId { get; set; }

        [JsonProperty("tail_point_stream_index")]
        public int TailPointStreamIndex { get; set; }

        [JsonProperty("tail_point_stream_index_threshold")]
        public int TailPointStreamIndexThreshold { get; set; }
    }
}
