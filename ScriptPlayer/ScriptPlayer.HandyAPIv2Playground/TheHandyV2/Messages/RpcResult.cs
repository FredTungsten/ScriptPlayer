using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class RpcResult
    {
        [JsonProperty("result")]
        public GenericResult Result { get; set; }
    }
}