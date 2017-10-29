using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace ScriptPlayer.Shared.Helpers
{
    public static class OsInformation
    {
        public static int GetWindowsReleaseVersion()
        {
            try
            {
                int releaseId = 0;

                releaseId = int.Parse(Registry
                    .GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", string.Empty)
                    .ToString());
                return releaseId;
            }
            catch (Exception)
            {
                // If we can't retreive a version, just skip the perm check entirely and don't allow Bluetooth usage.
                Debug.WriteLine("Can't get version!");
                return 0;
            }
        }
    }
}
