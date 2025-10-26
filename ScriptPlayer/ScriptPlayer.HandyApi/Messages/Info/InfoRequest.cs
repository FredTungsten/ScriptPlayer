using Newtonsoft.Json;

namespace ScriptPlayer.HandyApi.Messages
{
    public class InfoRequest
    {
        [GetParameter("timeout")]
        public int Timeout { get; set; }
    }

    public class InfoResponse
    {
        [JsonProperty("fw_status")]
        public int FirmwareStatus { get; set; }

        [JsonProperty("fw_version")]
        public string FirmwareVersion { get; set; }

        [JsonProperty("fw_feature_flags")]
        public string FirmwareFeatureFlags { get; set; }

        [JsonProperty("hw_model_no")]
        public int HardwareModelNo { get; set; }

        [JsonProperty("hw_model_name")]
        public string HardwareModelName { get; set; }

        [JsonProperty("hw_model_variant")]
        public int HardwareModelVariant { get; set; }
    }

    public class DeviceError
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("connected")]
        public bool Connected { get; set; }
    }

    public class GetModeResult
    {
        [JsonProperty("mode")]
        public int Mode { get; set; }

        [JsonProperty("mode_session_id")]
        public int ModeSessionId { get; set; }

    }

    public class PutModeRequest
    {
        [JsonProperty("mode")]
        public int Mode { get; set; }
    }

    public enum HandyModes : int
    {
        Hamp = 0,
        Hssp = 1,
        Hdsp = 2,
        Maintenance = 3,
        Hsp = 4,
        Ota = 5,
        Button = 6,
        Idel = 7,
        Vibrate = 8,
    }

    public class ModeResponse
    {
        [JsonProperty("mode")]
        public int Mode { get; set; }
        [JsonProperty("mode_session_id")]
        public int ModeSessionId { get; set; }
    }

    public class ConnectedResponse
    {
        [JsonProperty("connected")]
        public bool Connected { get; set; }
    }
}
