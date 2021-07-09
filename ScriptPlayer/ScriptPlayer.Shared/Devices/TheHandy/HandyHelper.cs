namespace ScriptPlayer.Shared.TheHandy
{
    public static class HandyHelper {
        private const string _connectionUrlBaseFormat = @"https://www.handyfeeling.com/api/v1/{0}/";

        private static string _connectionUrlWithId = null;
        private const string DefaultDeviceKey = "NO_KEY";
        public static bool IsDeviceKeySet => DeviceId != DefaultDeviceKey;

        public static string DefaultDeviceId => DefaultDeviceKey;
        public static string ConnectionBaseUrl {
            get
            {
                if (_updateConnectionUrl)
                {
                    _updateConnectionUrl = false;
                    _connectionUrlWithId = string.Format(_connectionUrlBaseFormat, DeviceId);
                }
                return _connectionUrlWithId;
            }
        }

        private static bool _updateConnectionUrl = true;
        private static string _deviceId = DefaultDeviceKey;
        public static string DeviceId {
            get => _deviceId;
            set
            {
                if (value != _deviceId)
                    _updateConnectionUrl = true;
                _deviceId = value;
            }
        }
    }
}