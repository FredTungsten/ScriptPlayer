using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using ScriptPlayer.Cli;

namespace ScriptPlayer.Ipc
{
    public static class SpConsole
    {
        public static void Run()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();

                //// Make sure the command line parser/quoter work as expected
                //string fake = QuoteArguments(args);
                //string[] fakeargs = CommandLineSplitter.CommandLineToArgs(fake);
                //bool yay = args.SequenceEqual(fakeargs);

                //if (!yay)
                //{
                //    throw new Exception("Command line arguments couln't be reconstructed :(");
                //}

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
                        if (args.Length > 2)
                        {
                            Console.WriteLine("Too many parameters for interactive mode");
                            return;
                        }

                        Console.WriteLine("# Interactive Mode - enter 'exit' to close");
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
                string commandLine = CommandLineHelper.ArgsToCommandline(args.Skip(2));
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

        private static void PrintHelp()
        {
            Console.WriteLine("ScriptPlayer Command Line Interface");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("Interactive Mode:    -i");
            Console.WriteLine("Single Command Mode: -c <Command> [Parameters]");
        }
    }
}
