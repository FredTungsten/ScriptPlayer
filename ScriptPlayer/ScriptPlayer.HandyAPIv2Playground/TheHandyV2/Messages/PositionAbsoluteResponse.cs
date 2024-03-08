using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class PositionAbsoluteResponse
    {
        [JsonProperty("result")]
        public GenericResult Result { get; set; }

        [JsonProperty("position")]
        public double Position { get; set; }
    }
}