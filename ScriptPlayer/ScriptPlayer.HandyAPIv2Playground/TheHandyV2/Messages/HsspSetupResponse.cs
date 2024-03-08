using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class HsspSetupResponse
    {
        [JsonProperty("result")]
        public HsspSetupResult Result { get; set; }
    }
}