using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class LoopSettingResponse
    {
        [JsonProperty("result")]
        public GenericResult Result { get; set; }

        [JsonProperty("activated")]
        public bool Activated { get; set; }
    }
}