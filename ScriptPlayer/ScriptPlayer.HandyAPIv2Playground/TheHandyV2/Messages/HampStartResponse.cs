using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class HampStartResponse
    {
        [JsonProperty("result")]
        public StateResult Result { get; set; }
    }
}