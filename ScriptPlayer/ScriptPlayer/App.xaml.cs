using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using ScriptPlayer.Shared.Helpers;
using ScriptPlayer.ViewModels;
using Application = System.Windows.Application;

namespace ScriptPlayer
{
    public partial class App : Application
    {
        public App()
        {
            DateTime start = DateTime.Now;

            if (AppDomain.CurrentDomain.FriendlyName.EndsWith(".vshost.exe"))
                return;

            Application.Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            if (!InstanceHandler.Startup("ScriptPlayer-Instance", "ScriptPlayer-CommandLinePipe"))
            {
                string debugMessage = $"CLI ExecutionTime = {(DateTime.Now - start).TotalSeconds:f3}s";

                Debug.WriteLine(debugMessage);
                Console.WriteLine(debugMessage);
                Environment.Exit(0);
            }
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
    }
}
