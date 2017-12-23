using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows;
using System.Windows.Threading;
using ScriptPlayer.Shared.Elevation;
using ScriptPlayer.Shared.Helpers;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ScriptPlayer
{
    public partial class App : Application
    {
        public App()
        {
            if (AppDomain.CurrentDomain.FriendlyName.EndsWith("vshost.exe")) return;

            Application.Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            string message = "Unhandled Exception!";
            if (args.ExceptionObject is Exception e)
                message = ExceptionHelper.BuildException(e);

            File.AppendAllText(Environment.ExpandEnvironmentVariables("%APPDATA%\\ScriptPlayer\\Crash.log"), message);
        }

        private void CurrentOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            string message = ExceptionHelper.BuildException(args.Exception);
            File.AppendAllText(Environment.ExpandEnvironmentVariables("%APPDATA%\\ScriptPlayer\\Crash.log"), message);
        }

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
            if (OsInformation.GetWindowsReleaseVersion() < 1703)
            {
                // Version too low to use Bluetooth, skip check.
                return;
            }

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

            if (!IsElevated()) return;

            LaunchCurrentAppAgain();
            Environment.Exit((int)result);
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
        private static bool IsElevated()
        {
            return Environment.GetCommandLineArgs().Contains("/elevated");
        }

        /// <summary>
        /// Determine if the current process was started with admin privileges
        /// </summary>
        /// <returns></returns>
        private static bool UserIsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Try to launch the program as admin, return result
        /// </summary>
        private static void LaunchCurrentAppAsAdmin()
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
