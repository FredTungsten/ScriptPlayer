using Newtonsoft.Json;

namespace ScriptPlayer.HandyApi.Messages
{
    public abstract class HsspSetupRequest
    {
    }

    public class HsspSetupUrlRequest : HsspSetupRequest
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
