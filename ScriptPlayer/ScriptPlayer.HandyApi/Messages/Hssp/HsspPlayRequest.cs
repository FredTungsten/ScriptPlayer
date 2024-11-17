using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptPlayer.HandyAPIv3Playground.TheHandyV3.Messages.Hssp
{
    public class HsspPlayRequest
    {
          [JsonProperty("start_time")]
        public int StartTime { get; set; }

        [JsonProperty("server_time")]
        public int ServerTime { get; set; }

        [JsonProperty("playback_rate")]
        public double PlaybackRate { get; set; }

        [JsonProperty("loop")]
        public bool Loop { get; set; }
    }
}
