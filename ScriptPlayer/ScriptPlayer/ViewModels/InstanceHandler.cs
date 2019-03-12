using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
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

            // entire commandline
            string commandLine = Environment.CommandLine;

            // pass it on to the other instance
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous))
            using (StreamWriter writer = new StreamWriter(client))
            {
                client.Connect(3000); // 3000ms timeout
                writer.WriteLine(commandLine);
                writer.Flush();
            }

            return false;
        }

        private static async void ProducerLoop()
        {
            try
            {
                while (!_shutdown)
                {
                    using (_pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.In, 10,
                        PipeTransmissionMode.Message, PipeOptions.Asynchronous))
                    {
                        await _pipeServer.WaitForConnectionAsync(_cancellationSource.Token);

                        if (!_pipeServer.IsConnected)
                            return;

                        using (StreamReader reader = new StreamReader(_pipeServer))
                        {
                            while (!reader.EndOfStream)
                            {
                                string commandLine = await reader.ReadLineAsync();
                                if (!string.IsNullOrWhiteSpace(commandLine))
                                    CommandLineQueue.Enqueue(commandLine);
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
                string line = CommandLineQueue.Deqeue();
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
