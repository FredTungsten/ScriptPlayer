using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class HsspPlay
    {
        /// <summary>
        /// The client side estimated server time in milliseconds (Unix Epoch).
        /// </summary>
        [JsonProperty("estimatedServerTime")]
        public int EstimatedServerTime { get; set; }

        /// <summary>
        /// The time index to start playing from in milliseconds.
        /// </summary>
        [JsonProperty("startTime")]
        public int StartTime { get; set; }
    }
}