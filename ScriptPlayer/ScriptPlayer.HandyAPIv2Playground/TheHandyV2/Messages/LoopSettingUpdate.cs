using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class LoopSettingUpdate
    {
        [JsonProperty("activated")]
        public bool Activated { get; set; }
    }
}