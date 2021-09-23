using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using ScriptPlayer.Ipc;
using ScriptPlayer.Shared;

namespace ScriptPlayer.ViewModels
{
    public class ExternalCommand
    {
        public string Command { get; private set; }

        public ActionResult Result { get; private set; }

        private ManualResetEvent ResetEvent { get; set; }

        public ExternalCommand(string command)
        {
            Command = command;
            ResetEvent = new ManualResetEvent(false);
        }

        public void SetResult(ActionResult result)
        {
            Result = result;
            ResetEvent.Set();
        }

        public ActionResult WaitForResult(TimeSpan timeout)
        {
            if (!ResetEvent.WaitOne(timeout))
                return new ActionResult(false, "Timeout");
            return Result;
        }
    }

    public static class InstanceHandler
    {
        private static readonly BlockingQueue<ExternalCommand> CommandLineQueue = new BlockingQueue<ExternalCommand>();

        public static event EventHandler<ExternalCommand> CommandLineReceived;

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
                io.WriteString($"OpenFile \"{args[1]}\"");
            }

            return false;
        }

        private static async void ProducerLoop()
        {
            try
            {
                while (!_shutdown)
                {
                    try
                    {
                        using (_pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 10,
                            PipeTransmissionMode.Message, PipeOptions.Asynchronous))
                        {
                            await _pipeServer.WaitForConnectionAsync(_cancellationSource.Token);

                            if (!_pipeServer.IsConnected || _shutdown)
                                return;

                            StreamString io = new StreamString(_pipeServer);
                            while (_pipeServer.IsConnected)
                            {
                                string commandLine = io.ReadString();
                                if (commandLine == null || _shutdown)
                                    break;

                                Debug.WriteLine("Command received via NamedPipe: " + commandLine);
                                if (!string.IsNullOrWhiteSpace(commandLine))
                                {
                                    ExternalCommand command = new ExternalCommand(commandLine);
                                    CommandLineQueue.Enqueue(command);

                                    var result = command.WaitForResult(TimeSpan.FromMilliseconds(2000));
                                    io.WriteString((result.Success ? "OK" : "FAIL") + ":" + result.Message);
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine("InstanceHandler.ProducerLoop: " + ex.Message);
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
                ExternalCommand command = CommandLineQueue.Dequeue();
                if (command == null)
                    return;

                OnCommandLineReceived(command);
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

        private static void OnCommandLineReceived(ExternalCommand command)
        {
            CommandLineReceived?.Invoke(null, command);
        }
    }
}
