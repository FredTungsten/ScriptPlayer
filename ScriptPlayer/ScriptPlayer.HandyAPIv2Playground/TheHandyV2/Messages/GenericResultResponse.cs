using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class GenericResultResponse
    {
        [JsonProperty("result")]
        public GenericResult Result { get; set; }
    }
}