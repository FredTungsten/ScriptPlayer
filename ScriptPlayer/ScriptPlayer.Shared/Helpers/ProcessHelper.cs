using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ScriptPlayer.Shared.Helpers
{
    public static class ProcessHelper
    {
        //https://stackoverflow.com/a/52098738

        public static bool IsExecutableRunning(string exePath)
        {
            string name = Path.GetFileNameWithoutExtension(exePath);
            return CheckRunningProcess(name, exePath);
        }

        [DllImport("kernel32.dll")]
        private static extern bool QueryFullProcessImageName(IntPtr hprocess, int dwFlags, StringBuilder lpExeName, out int size);

        private static bool CheckRunningProcess(string processName, string path)
        {

            Process[] processes = Process.GetProcessesByName(processName);
            foreach (Process process in processes)
            {
                try
                {
                    var fileName = GetMainModuleFileName(process);
                    if (fileName == null)
                        continue;

                    if (PathsEqual(fileName, path))
                    {
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        private static bool PathsEqual(string file1, string file2)
        {
            return string.Equals(
                Path.GetFullPath(file1),
                Path.GetFullPath(file2),
                StringComparison.InvariantCultureIgnoreCase);
        }

        // Get x64 process module name from x86 process
        private static string GetMainModuleFileName(Process process, int buffer = 1024)
        {

            var fileNameBuilder = new StringBuilder(buffer);
            int bufferLength = fileNameBuilder.Capacity + 1;
            return QueryFullProcessImageName(process.Handle, 0, fileNameBuilder, out bufferLength) ?
                fileNameBuilder.ToString() :
                null;
        }
    }
}
