using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class ConnectedResponse
    {
        [JsonProperty("connected")]
        public bool Connected { get; set; }
    }
}