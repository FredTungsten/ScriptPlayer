using System;
using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class ModeUpdate
    {
        [JsonProperty("mode")]
        public Mode Mode { get; set; }
    }
}
