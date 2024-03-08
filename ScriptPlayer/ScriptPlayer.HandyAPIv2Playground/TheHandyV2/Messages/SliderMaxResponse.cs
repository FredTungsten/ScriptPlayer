using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class SliderMaxResponse
    {
        [JsonProperty("max")]
        public double Max { get; set; }
    }
}