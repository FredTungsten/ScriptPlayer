using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptPlayer.HandyAPIv3Playground.TheHandyV3.Messages.Hssp
{
    public class HsspSyncTimeRequest
    {
        [JsonProperty("current_time")]
        public int CurrentTime { get; set; }
        
        [JsonProperty("server_time")]
        public int ServerTime { get; set; }

        [JsonProperty("filter")]
        public double Filter {  get; set; }
    }
}
