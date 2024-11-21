using Newtonsoft.Json;

namespace ScriptPlayer.HandyApi.Messages
{
    internal class HampVelocityRequest
    {
        [JsonProperty("velocity")]
        public double Velocity { get; set; }
    }
}