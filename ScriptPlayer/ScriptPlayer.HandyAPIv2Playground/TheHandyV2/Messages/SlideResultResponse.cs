using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class SlideResultResponse
    {
        [JsonProperty("result")]
        public SlideResult Result { get; set; }
    }
}