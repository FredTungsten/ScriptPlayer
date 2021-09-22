using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ScriptPlayer.Cli
{
    public class ConsoleHelper
    {
        private static bool _created;

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AttachConsole(int processId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        
        [DllImport("Kernel32")]
        public static extern void FreeConsole();

        const int STD_OUTPUT_HANDLE = -11;

        public static bool EnsureConsole()
        {
            // Command line given, display console
            if (AttachConsole(-1))
            {
                var stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);
                var safeFileHandle = new SafeFileHandle(stdHandle, true);
                var fileStream = new FileStream(safeFileHandle, FileAccess.Write);
                var standardOutput = new StreamWriter(fileStream) { AutoFlush = true };
                Console.SetOut(standardOutput);
                return true; // Attach to an parent process console
            }

            if (AllocConsole()) // Alloc a new console
            {
                _created = true;
                return true;
            }

            return false;
        }

        public static void CleanUp()
        {
            if (_created)
            {
                FreeConsole();
            }
        }
    }
}
