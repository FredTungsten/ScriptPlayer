using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class ErrorResponse
    {
        [JsonProperty("error")]
        public ErrorDetails Error { get; set; }
    }
}