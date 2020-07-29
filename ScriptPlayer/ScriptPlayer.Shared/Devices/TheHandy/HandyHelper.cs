namespace ScriptPlayer.Shared.Devices.TheHandy
{
    public static class HandyHelper {
        private const string _connectionUrlBaseFormat = @"https://www.handyfeeling.com/api/v1/{0}/";
        private static string _connectionUrlWithId = null;
        private const string _defaultKey = "NO_KEY";
        public static bool IsDeviceIdSet => DeviceId != _defaultKey;

        public static string Default => _defaultKey;
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
        private static string _deviceId = _defaultKey;
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