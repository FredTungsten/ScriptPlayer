using Newtonsoft.Json;

namespace ScriptPlayer.HandyApi.Messages
{
    public class SliderStateResponse
    {
        [JsonProperty("position")]
        public double Position { get; set; }

        [JsonProperty("position_absolute")]
        public double PositionAbsolute { get; set; }

        [JsonProperty("motor_temp")]
        public int MotorTemp { get; set; }

        [JsonProperty("speed_absolute")]
        public int SpeedAbsolute { get; set; }

        [JsonProperty("dir")]
        public bool Direction { get; set; }

        [JsonProperty("motor_position")]
        public int MotorPosition { get; set; }
    }
}