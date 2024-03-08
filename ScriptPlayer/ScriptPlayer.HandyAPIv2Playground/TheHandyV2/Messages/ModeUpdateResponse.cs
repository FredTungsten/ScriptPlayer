using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class ModeUpdateResponse
    {
        [JsonProperty("result")]
        public ModeResult Result { get; set; }
    }
}