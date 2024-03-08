using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class SliderMinResponse
    {
        [JsonProperty("min")]
        public double Min { get; set; }
    }
}