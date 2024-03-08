using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class HsspPlayResponse
    {
        [JsonProperty("result")]
        public HsspPlayResult Result { get; set; }
    }
}