using Newtonsoft.Json;

namespace ScriptPlayer.HandyAPIv3Playground.TheHandyV3
{
    internal class HampVelocityRequest
    {
        [JsonProperty("velocity")]
        public double Velocity { get; set; }
    }
}