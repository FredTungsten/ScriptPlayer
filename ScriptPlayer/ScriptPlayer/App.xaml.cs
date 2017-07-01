using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using ScriptPlayer.Shared.Elevation;

namespace ScriptPlayer
{
    public partial class App : Application
    { 
        protected override void OnStartup(StartupEventArgs e)
        {

#if DEBUGELEVATED
            if (IsElevated())
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                else
                    Debugger.Launch();
            }
#endif

            string exeName = Path.GetFileName(PermissionChecker.GetExe());
            Guid guid = Guid.NewGuid();

            Debug.WriteLine(@"Is Admin: " + UserIsAdmin());

            var result = PermissionChecker.EnsureAppPermissionsSet(exeName, guid);

            if (result == PermissionCheckResult.ElevationRequired)
            {
                if (IsElevated() || UserIsAdmin())
                {
                    MessageBox.Show("Failed to set BLE Permissions!", "Failed", MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    Environment.Exit(1);
                }
                else
                {
                    if (MessageBox.Show("BLE permissions requried. Restart now with elevated privilegues?",
                            "Register BLE now?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        LaunchCurrentAppAsAdmin();
                        Environment.Exit(2);
                    }
                }
            }

            if (IsElevated())
            {
                LaunchCurrentAppAgain();
                Environment.Exit((int)result);
            }
        }

        private void LaunchCurrentAppAgain()
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = PermissionChecker.GetExe(),
                UseShellExecute = true
            };

            try
            {
                Process process = new Process { StartInfo = info };
                process.Start();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Determine if the current process was started with the "/elevated" argument
        /// </summary>
        private bool IsElevated()
        {
            return Environment.GetCommandLineArgs().Contains("/elevated");
        }

        /// <summary>
        /// Determine if the current process was started with admin privileges
        /// </summary>
        /// <returns></returns>
        private bool UserIsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Try to launch the program as admin, return result
        /// </summary>
        private void LaunchCurrentAppAsAdmin()
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = PermissionChecker.GetExe(),
                UseShellExecute = true,
                CreateNoWindow = true
            };

            //Required by UAC ;(
            info.UseShellExecute = true;

            // Provides Run as Administrator
            info.Verb = "runas";

            // Custom argument so we know we manually elevated the process
            info.Arguments = "/elevated";

            try
            {
                Process process = new Process { StartInfo = info };
                process.Start();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }
}
