using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class InfoResponse
    {
        [JsonProperty("fwVersion")]
        public string FirmwareVersion { get; set; }

        [JsonProperty("fwStatus")]
        public FirmwareStatus FirmwareStatus { get; set; }

        [JsonProperty("hwVersion")]
        public string HardwareVersion { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("branch")]
        public string FirmwareBranch { get; set; }

        [JsonProperty("sessionId")]
        public string SessionId { get; set; }
    }
}