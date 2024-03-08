using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class HampStopResponse
    {
        [JsonProperty("result")]
        public StateResult Result { get; set; }
    }
}