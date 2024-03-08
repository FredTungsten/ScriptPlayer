using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class StatusResponse
    {
        [JsonProperty("mode")]
        public Mode Mode { get; set; }

        /// <summary>
        /// Can be <see cref="HsspState"/> or <see cref="HampState"/> depending on <see cref="Mode"/>
        /// </summary>
        [JsonProperty("state")]
        public int State { get; set; }
    }
}