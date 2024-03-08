using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class HampVelocityPercentResponse
    {
        [JsonProperty("result")]
        public GenericResult Result { get; set; }

        [JsonProperty("velocity")]
        public double Velocity { get; set; }
    }
}