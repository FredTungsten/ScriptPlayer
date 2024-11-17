using Newtonsoft.Json;

namespace ScriptPlayer.HandyAPIv3Playground.TheHandyV3.Messages.Hssp
{
    public abstract class HsspSetupRequest
    {
    }

    public class HsspSetpUrlRequest
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
