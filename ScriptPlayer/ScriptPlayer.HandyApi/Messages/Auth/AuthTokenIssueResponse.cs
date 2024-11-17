using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ScriptPlayer.HandyAPIv3Playground.TheHandyV3.Messages
{

    public class AuthTokenIssueResponse
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("renew")]
        public string Renew { get; set; }
    }
}
