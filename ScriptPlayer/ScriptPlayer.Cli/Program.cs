using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;

namespace ScriptPlayer.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            // entire commandline
            string commandLine = Environment.CommandLine;

            // pass it on to the other instance
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "ScriptPlayer-CommandLinePipe", PipeDirection.Out, PipeOptions.Asynchronous))
            using (StreamWriter writer = new StreamWriter(client))
            {
                try
                {
                    client.Connect(3000); // 3000ms timeout
                    writer.WriteLine(commandLine);
                    writer.Flush();

                    Debug.WriteLine("Commandline successfully sent to ScriptPlayer");
                }
                catch(Exception e)
                {
                    Debug.WriteLine("Couldn't send commandline to ScriptPlayer: " + e.Message);
                }
            }
        }
    }
}
