using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class HampVelocityPercent
    {
        [JsonProperty("velocity")]
        public double Velocity { get; set; }
    }
}