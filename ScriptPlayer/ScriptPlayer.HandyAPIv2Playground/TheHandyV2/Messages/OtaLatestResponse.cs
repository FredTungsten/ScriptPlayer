using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class OtaLatestResponse
    {
        [JsonProperty("fwVersion")]
        public string FirmwareVersion { get; set; }
    }
}