using Newtonsoft.Json;

namespace ScriptPlayer.HandyAPIv3Playground.TheHandyV3.Messages.Utils
{
    public class ServertimeResponse
    {
        [JsonProperty("server_time")]
        public long ServerTime { get; set; }
    }
}
