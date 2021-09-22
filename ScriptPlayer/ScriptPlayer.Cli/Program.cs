using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ScriptPlayer.Ipc;

namespace ScriptPlayer.Cli
{
    class Program
    {
        static void Main()
        {
            try
            {
                string[] args = (Environment.GetCommandLineArgs());

                // Make sure the command line parser/quoter work as expected
                string fake = QuoteArguments(args);
                string[] fakeargs = CommandLineSplitter.CommandLineToArgs(fake);
                bool yay = args.SequenceEqual(fakeargs);

                if (!yay)
                {
                    throw new Exception("Command line arguments couln't be reconstructed :(");
                }

                if (args.Length < 2)
                    return;

                string myArgument = args[1];
                bool interactive;

                switch (myArgument)
                {
                    case "-c":
                        interactive = false;
                        if (args.Length < 3)
                        {
                            Console.WriteLine("Not enought parameters for command mode");
                            return;
                        }

                        break;
                    case "-i":
                        interactive = true;
                        //ConsoleHelper.EnsureConsole();
                        if (args.Length > 2)
                        {
                            Console.WriteLine("Too many parameters for interactive mode");
                            return;
                        }

                        Console.WriteLine("# Interactive Mode");
                        break;
                    case "-h":
                        PrintHelp();
                        return;
                    default:
                        Console.WriteLine($"Unknown mode switch '{myArgument}'");
                        PrintHelp();
                        return;
                }

                // Only pass on the other arguments
                string commandLine = QuoteArguments(args.Skip(2));
                if (string.IsNullOrEmpty(commandLine) && !interactive)
                    return;

                // pass it on to the other instance
                using (NamedPipeClientStream client = new NamedPipeClientStream(".", "ScriptPlayer-CommandLinePipe",
                    PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    StreamString io = new StreamString(client);
                    try
                    {
                        client.Connect(500); // 500ms timeout

                        do
                        {
                            if (interactive)
                            {
                                commandLine = Console.ReadLine();
                                if (commandLine == "exit")
                                    break;
                            }

                            io.WriteString(commandLine);

                            string response = io.ReadString();
                            Console.WriteLine(response);

                            Debug.WriteLine("Commandline successfully sent to ScriptPlayer");
                        } while (interactive && commandLine != "exit");
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Couldn't send commandline to ScriptPlayer: " + e.Message);
                    }
                }
            }
            finally
            {
                ConsoleHelper.CleanUp();
            }
        }

        private static string QuoteArguments(IEnumerable<string> args)
        {
            return string.Join(" ", args.Select(QuoteArg));
        }

        private static string QuoteArg(string arg)
        {
            // https://docs.microsoft.com/en-gb/archive/blogs/twistylittlepassagesallalike/everyone-quotes-command-line-arguments-the-wrong-way

            if (arg.IndexOfAny(new[] {' ', '\t', '\n', '\v', '\"'}) < 0)
                return arg;

            StringBuilder b = new StringBuilder();

            b.Append('"');

            for (int index = 0;; ++index)
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

        private static void PrintHelp()
        {
            throw new NotImplementedException();
        }
    }

    // https://stackoverflow.com/a/749653/3214843
    public static class CommandLineSplitter
    {
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
