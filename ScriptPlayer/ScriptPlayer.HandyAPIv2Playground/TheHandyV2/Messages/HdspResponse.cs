using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class HdspResponse
    {
        [JsonProperty("result")]
        public HdspResult Result { get; set; }
    }
}