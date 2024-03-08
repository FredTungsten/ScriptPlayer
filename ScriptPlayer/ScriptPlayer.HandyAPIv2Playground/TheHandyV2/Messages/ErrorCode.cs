namespace ScriptPlayer.Shared.TheHandyV2
{
    public enum ErrorCode
    {
        // Base Mode

        UnspecificError = 2000,
        InvalidRequest = 2001,
        MethodNotFound = 2002,

        // Hamp Mode

        UnspecificHampError = 3000,

        // Hssp Mode

        UnspecificHsspError = 4000,
        DownloadFailed = 4001,
        HashError = 4002,
        SyncRequired = 4003,
        TokenError = 4004,
        MaxScriptSizeError = 4005,
        DeviceStorageFullError = 4006,
        DeviceStorageFreeError = 4007,
        DeviceStorageCleanError = 4008,

        // Hdsp Mode

        UnspecificHdspError = 5000,

        // Maintenance Mode

        UnspecificMaintenanceError = 6000,
        OperationFailed = 6001,
    }
}