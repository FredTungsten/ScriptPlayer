using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class GenericError
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        //"data"
    }
}