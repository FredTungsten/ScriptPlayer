using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ScriptPlayer.Ipc
{
    public static class CommandLineHelper
    {
        public static string ArgsToCommandline(IEnumerable<string> args)
        {
            return string.Join(" ", args.Select(QuoteArg));
        }

        private static string QuoteArg(string arg)
        {
            // https://docs.microsoft.com/en-gb/archive/blogs/twistylittlepassagesallalike/everyone-quotes-command-line-arguments-the-wrong-way

            if (arg.IndexOfAny(new[] { ' ', '\t', '\n', '\v', '\"' }) < 0)
                return arg;

            StringBuilder b = new StringBuilder();

            b.Append('"');

            for (int index = 0; ; ++index)
            {
                int backslashes = 0;
                while (index != arg.Length && arg[index] == '\\')
                {
                    ++index;
                    ++backslashes;
                }

                if (index == arg.Length)
                {
                    b.Append('\\', backslashes * 2);
                    break;
                }
                else if (arg[index] == '"')
                {
                    b.Append('\\', backslashes * 2 + 1);
                    b.Append(arg[index]);
                }
                else
                {
                    b.Append('\\', backslashes);
                    b.Append(arg[index]);
                }
            }

            b.Append('"');

            return b.ToString();
        }
    

        // https://stackoverflow.com/a/749653/3214843

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        public static string[] CommandLineToArgs(string commandLine)
        {
            IntPtr argv = CommandLineToArgvW(commandLine, out int argc);
            if (argv == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception();

            try
            {
                string[] args = new string[argc];
                for (int i = 0; i < args.Length; i++)
                {
                    IntPtr p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }
    }
}
