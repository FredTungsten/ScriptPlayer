using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class ErrorDetails
    {
        [JsonProperty("connected")]
        public bool Connected { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("code")]
        public ErrorCode Code { get; set; }
    }
}