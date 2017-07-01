using System;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using Microsoft.Win32;

namespace ScriptPlayer.Shared.Elevation
{
    public static class PermissionChecker
    {
        //private static readonly byte[] StaticPermissions =
        //{
        //    0x01, 0x00, 0x04, 0x80, 0x9C, 0x00, 0x00, 0x00, 0xAC, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //    0x14, 0x00, 0x00, 0x00, 0x02, 0x00, 0x88, 0x00, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, 0x00,
        //    0x07, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x0A, 0x00, 0x00, 0x00,
        //    0x00, 0x00, 0x14, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05,
        //    0x12, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x00, 0x07, 0x00, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00,
        //    0x00, 0x00, 0x00, 0x05, 0x20, 0x00, 0x00, 0x00, 0x20, 0x02, 0x00, 0x00, 0x00, 0x00, 0x18, 0x00,
        //    0x03, 0x00, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0F, 0x02, 0x00, 0x00, 0x00,
        //    0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00,
        //    0x00, 0x00, 0x00, 0x05, 0x13, 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, 0x00, 0x03, 0x00, 0x00, 0x00,
        //    0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x14, 0x00, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00,
        //    0x00, 0x00, 0x00, 0x05, 0x20, 0x00, 0x00, 0x00, 0x20, 0x02, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00,
        //    0x00, 0x00, 0x00, 0x05, 0x20, 0x00, 0x00, 0x00, 0x20, 0x02, 0x00, 0x00
        //};

        public static PermissionCheckResult EnsureAppPermissionsSet(string exeName, Guid defaultGuid)
        {
            string fallbackGuid = defaultGuid.ToString("B").ToUpper();

            RegistryHelper helper = new RegistryHelper(RegistryView.Default);

            string rootKeyPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\AppID";
            string registryKey = $"{rootKeyPath}\\{exeName}";

            const string constAppId = "AppID";
            const string constPermissions = "AccessPermission";

            string actualGuid = "";
            bool modified = false;

            if (!helper.Exists(registryKey, constAppId))
            {
                RegistryValue value = new RegistryValue(registryKey, constAppId)
                {
                    Type = RegistryValueKind.String,
                    Value = fallbackGuid
                };

                var result = helper.Write(value);

                if (result == RegistryKeyResult.ResultState.AccessDenied)
                    return PermissionCheckResult.ElevationRequired;

                actualGuid = (string)value.Value;
                modified = true;
            }
            else
            {
                RegistryValue guid = helper.Read(registryKey, constAppId);
                if (guid.Type == RegistryValueKind.String)
                    actualGuid = (string)guid.Value;
            }

            string appIdPath = $"{rootKeyPath}\\{actualGuid}";

            byte[] permissions;

            if (!helper.Exists(appIdPath))
            {
                permissions = CreateAccessPermissions();

                RegistryValue value = new RegistryValue(appIdPath, constPermissions)
                {
                    Type = RegistryValueKind.Binary,
                    Value = permissions
                };

                var result = helper.Write(value);

                if (result == RegistryKeyResult.ResultState.AccessDenied)
                    return PermissionCheckResult.ElevationRequired;

                return PermissionCheckResult.PermissionsCreated;
            }
            else
            {
                RegistryValue value = helper.Read(appIdPath, constPermissions);
                if (value.Type == RegistryValueKind.Binary)
                {
                    permissions = (byte[])value.Value;

                    if (PermissionsSufficient(permissions))
                    {
                        if(modified)
                            return PermissionCheckResult.PermissionsModified;
                        return PermissionCheckResult.PermissionsExist;
                    }
                }

                permissions = CreateAccessPermissions();

                RegistryValue permissionsValue =
                    new RegistryValue(appIdPath, constPermissions)
                    {
                        Type = RegistryValueKind.Binary,
                        Value = permissions
                    };

                var result = helper.Write(permissionsValue);

                if (result == RegistryKeyResult.ResultState.AccessDenied)
                    return PermissionCheckResult.ElevationRequired;

                return PermissionCheckResult.PermissionsModified;
            }
        }

        private static bool PermissionsSufficient(byte[] permissions)
        {
            // https://msdn.microsoft.com/en-us/library/cc230374.aspx
            RawSecurityDescriptor descriptor = new RawSecurityDescriptor(permissions, 0);

            //TODO: Check if the permissions are sufficient, not just if they match the expected ones.

            return permissions.SequenceEqual(CreateAccessPermissions());
        }

        private static byte[] CreateAccessPermissions()
        {
            //return StaticPermissions;

            // https://deploywindows.info/2013/10/17/how-to-build-a-sddl-string-and-set-service-permissions/
            // https://social.msdn.microsoft.com/Forums/en-US/58da3fdb-a0e1-4161-8af3-778b6839f4e1/bluetooth-bluetoothledevicefromidasync-does-not-complete-on-10015063?forum=wdk#ef927009-676c-47bb-8201-8a80d2323a7f

            RawSecurityDescriptor descriptor = new RawSecurityDescriptor("O:BAG:BAD:(A;;0x7;;;PS)(A;;0x3;;;SY)(A;;0x7;;;BA)(A;;0x3;;;AC)(A;;0x3;;;LS)(A;;0x3;;;NS)");
            byte[] data = new byte[descriptor.BinaryLength];
            descriptor.GetBinaryForm(data,0);
            return data;
        }

        public static string GetExe()
        {
            return Assembly.GetEntryAssembly().Location;
            //return Assembly.GetExecutingAssembly().Location;
        }
    }
}
