using Newtonsoft.Json;

namespace ScriptPlayer.HandyApi.Messages
{
    public class ServertimeResponse
    {
        [JsonProperty("server_time")]
        public long ServerTime { get; set; }
    }
}
