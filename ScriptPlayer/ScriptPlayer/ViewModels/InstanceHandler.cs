using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using ScriptPlayer.Ipc;
using ScriptPlayer.Shared;

namespace ScriptPlayer.ViewModels
{
    public static class InstanceHandler
    {
        private static readonly BlockingQueue<string> CommandLineQueue = new BlockingQueue<string>();

        public static event EventHandler<string> CommandLineReceived;

        private static Mutex _singleInstanceMutex;
        private static NamedPipeServerStream _pipeServer;
        private static bool _enabled;
        private static bool _shutdown;

        private static Thread _consumerThread;
        private static Thread _producerThread;
        private static CancellationTokenSource _cancellationSource;
        private static string _pipeName;

        // https://stackoverflow.com/questions/184084/how-to-force-c-sharp-net-app-to-run-only-one-instance-in-windows

        public static bool Startup(string instanceName, string pipeName)
        {
            _pipeName = pipeName;
            _cancellationSource = new CancellationTokenSource();
            _singleInstanceMutex = new Mutex(true, instanceName, out bool createdNew);

            if (createdNew)
            {
                _producerThread = new Thread(ProducerLoop);
                _producerThread.Start();
                
                return true;
            }

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length != 2)
                return false;
            
            // pass file on to the other instance
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous))
            {
                StreamString io = new StreamString(client);
                client.Connect(500); // 500ms timeout
                io.WriteString($"OpenFile \"{args[0]}\"");
            }

            return false;
        }

        private static async void ProducerLoop()
        {
            try
            {
                while (!_shutdown)
                {
                    using (_pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 10,
                        PipeTransmissionMode.Message, PipeOptions.Asynchronous))
                    {
                        await _pipeServer.WaitForConnectionAsync(_cancellationSource.Token);

                        if (!_pipeServer.IsConnected)
                            return;

                        StreamString io = new StreamString(_pipeServer);
                        while (_pipeServer.IsConnected)
                        {
                            string commandLine = io.ReadString();
                            if (commandLine == null)
                                break;

                            Debug.WriteLine("Command received via NamedPipe: " + commandLine);
                            if (!string.IsNullOrWhiteSpace(commandLine))
                            {
                                CommandLineQueue.Enqueue(commandLine);
                                io.WriteString("OK");
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (ThreadAbortException) { }
        }

        public static void EnableEvents()
        {
            if (_enabled || _shutdown)
                return;

            _enabled = true;
            _consumerThread = new Thread(ConsumerLoop);
            _consumerThread.Start();
        }

        private static void ConsumerLoop()
        {
            while (!_shutdown)
            {
                string line = CommandLineQueue.Dequeue();
                if (line == null)
                    return;

                OnCommandLineReceived(line);
            }
        }

        public static void Shutdown()
        {
            if (!_enabled)
                return;

            _shutdown = true;

            _cancellationSource.Cancel();
            
            _singleInstanceMutex.ReleaseMutex();
            _singleInstanceMutex.Dispose();

            CommandLineQueue.Close();

            if (!_producerThread.Join(TimeSpan.FromSeconds(2)))
                _producerThread.Abort();

            if (!_consumerThread.Join(TimeSpan.FromSeconds(2)))
                _consumerThread.Abort();

            _enabled = false;
        }

        private static void OnCommandLineReceived(string commandLine)
        {
            CommandLineReceived?.Invoke(null, commandLine);
        }
    }
}
