using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using ScriptPlayer.Cli;

namespace ScriptPlayer.Ipc
{
    public static class SpConsole
    {
        public static void Run(bool assumeInteractive)
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

                string myArgument = args.Length > 1 ? args[1] : "";
                bool interactive;
                int argnum = 2;

                switch (myArgument)
                {
                    case "-c":
                        interactive = false;
                        break;
                    case "-i":
                        interactive = true;
                        break;
                    case "-h":
                        PrintHelp();
                        return;
                    default:
                        interactive = assumeInteractive;
                        argnum = 1;
                        break;
                }

                // Only pass on the other arguments
                string commandLine = CommandLineHelper.ArgsToCommandline(args.Skip(argnum));
                if (string.IsNullOrEmpty(commandLine) && !interactive)
                    return;

                if(interactive)
                    Console.WriteLine("# Interactive Mode - enter 'exit' to close");

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
                        Console.Error.WriteLine(e.Message);
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
            Console.WriteLine("Show this:           -h");
        }
    }
}
