using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class UpdatePerform
    {
        [JsonProperty("fwVersion")]
        public string FirmwareVersion { get; set; }
    }
}