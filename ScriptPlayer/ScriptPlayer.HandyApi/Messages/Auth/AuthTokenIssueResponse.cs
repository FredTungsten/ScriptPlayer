using Newtonsoft.Json;

namespace ScriptPlayer.HandyApi.Messages.Auth
{

    public class AuthTokenIssueResponse
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("renew")]
        public string Renew { get; set; }
    }
}
