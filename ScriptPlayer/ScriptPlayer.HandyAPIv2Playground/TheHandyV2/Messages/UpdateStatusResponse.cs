using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class UpdateStatusResponse
    {
        [JsonProperty("result")]
        public GenericResult Result { get; set; }

        [JsonProperty("status")]
        public StatusResult Status { get; set; }

        [JsonProperty("progress")]
        public int Progress { get; set; }
    }
}