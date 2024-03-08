using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class SyncResponse
    {
        [JsonProperty("result")]
        public GenericResult Result { get; set; }

        [JsonProperty("dtserver")]
        public int Timestamp { get; set; }
    }
}