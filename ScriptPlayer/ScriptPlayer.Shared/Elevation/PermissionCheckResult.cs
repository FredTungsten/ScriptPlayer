namespace ScriptPlayer.Shared.Elevation
{
    public enum PermissionCheckResult
    {
        PermissionsExist = 0,
        PermissionsCreated = 1,
        PermissionsModified = 2,
        ElevationRequired = 3,
        ElevationFailed = 4,
        ElevationInsufficient = 5
    }
}