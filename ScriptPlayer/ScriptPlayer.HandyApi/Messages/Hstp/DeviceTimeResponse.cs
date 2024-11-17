using Newtonsoft.Json;

namespace ScriptPlayer.HandyAPIv3Playground.TheHandyV3
{
    public class DeviceTimeResponse
    {
        /*
         "time": 769799,
         "clock_offset": 1707836664395,
         "rtd": 100
         */

        [JsonProperty("time")]
        public long Time { get; set; }

        [JsonProperty("clock_offset")]
        public long ClockOffset { get; set; }

        [JsonProperty("rtd")]
        public int RoundTrimDelay { get; set; }
    }
}