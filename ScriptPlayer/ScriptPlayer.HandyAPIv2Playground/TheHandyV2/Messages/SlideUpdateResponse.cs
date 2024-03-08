using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class SlideUpdateResponse
    {
        [JsonProperty("result")]
        public SlideResult Result { get; set; }
    }
}