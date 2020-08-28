using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using ScriptPlayer.Shared.Interfaces;

namespace ScriptPlayer.Shared
{

    public abstract class Device : IActionBasedDevice, INotifyPropertyChanged, IDisposable
    {
        public event EventHandler<Exception> Disconnected;

        protected virtual void OnDisconnected(Exception e)
        {
            Dispose();
            Disconnected?.Invoke(this, e);
        }

        private bool _running;
        private bool _isEnabled;

        private readonly Thread _commandThread;
        private readonly BlockingQueue<QueueEntry<DeviceCommandInformation>> _queue = new BlockingQueue<QueueEntry<DeviceCommandInformation>>();

        public TimeSpan MinDelayBetweenCommands = TimeSpan.FromMilliseconds(166);
        public TimeSpan AcceptableCommandExecutionDelay = TimeSpan.FromMilliseconds(5);

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (value == _isEnabled) return;
                _isEnabled = value;

                if (!_isEnabled)
                    Stop();

                OnPropertyChanged();
            }
        }

        public string Name { get; set; }

        protected Device()
        {
            _running = true;
            _commandThread = new Thread(CommandLoop);
            _commandThread.Start();
        }

        private async void CommandLoop()
        {
            while (_running)
            {
                var entry = _queue.Dequeue();

                if (!_isEnabled)
                    continue;

                if (entry == null)
                    return;

                DateTime now = DateTime.Now;
                TimeSpan delay = now - entry.Submitted;

                if (delay > AcceptableCommandExecutionDelay)
                    Debug.WriteLine("Command Execution Delay: " + delay.ToString("g"));

                DeviceCommandInformation information = entry.Values;
                await Set(information);

                TimeSpan wait = DateTime.Now - now;
                if (wait < MinDelayBetweenCommands)
                    await Task.Delay(MinDelayBetweenCommands - wait);
            }
        }

        public void Close()
        {
            _running = false;
            _queue.Close();

            if (!_commandThread.Join(TimeSpan.FromMilliseconds(500)))
                _commandThread.Abort();
        }

        public void Enqueue(DeviceCommandInformation information)
        {
            _queue.ReplaceExisting(new QueueEntry<DeviceCommandInformation>(information), CompareCommandInformation);
        }

        private bool CompareCommandInformation(QueueEntry<DeviceCommandInformation> arg1, QueueEntry<DeviceCommandInformation> arg2)
        {
            return CommandsAreSimilar(arg1.Values, arg2.Values);
        }

        protected virtual bool CommandsAreSimilar(DeviceCommandInformation command1, DeviceCommandInformation command2)
        {
            return Math.Abs(command1.PositionToTransformed - command2.PositionToTransformed) < 10;
        }

        protected abstract Task Set(DeviceCommandInformation information);

        public abstract Task Set(IntermediateCommandInformation information);

        protected abstract void StopInternal();

        public void Stop()
        {
            EmptyQueue();
            StopInternal();
        }

        private void EmptyQueue()
        {
            _queue.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual void Dispose()
        {
            Close();
        }

        public virtual void SetMinCommandDelay(TimeSpan settingsCommandDelay)
        {
            MinDelayBetweenCommands = settingsCommandDelay;
        }
    }
}
