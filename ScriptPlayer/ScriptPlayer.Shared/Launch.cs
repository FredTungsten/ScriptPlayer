using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace ScriptPlayer.Shared
{
    public class Launch
    {
        public event EventHandler<Exception> Disconnected; 

        private BluetoothLEDevice _device;
        private readonly GattCharacteristic _commandCharacteristics;
        private readonly GattCharacteristic _notifyCharacteristics;
        private readonly GattCharacteristic _writeCharacteristics;
        private bool _initialized;
        private Thread _commandThread;
        private BlockingQueue<QueueEntry> _queue = new BlockingQueue<QueueEntry>();
        private bool _running;

        public Launch(BluetoothLEDevice device, GattCharacteristic writeCharacteristics, GattCharacteristic notifyCharacteristics, GattCharacteristic commandCharacteristics)
        {
            _device = device;
            _writeCharacteristics = writeCharacteristics;
            _notifyCharacteristics = notifyCharacteristics;
            _commandCharacteristics = commandCharacteristics;

            _running = true;
            _commandThread = new Thread(CommandLoop);
            _commandThread.Start();
        }

        public void Close()
        {
            _running = false;
            _queue.Close();
        }

        private async void CommandLoop()
        {
            TimeSpan showDelay = TimeSpan.FromMilliseconds(1);

            while (_running)
            {
                var entry = _queue.Deqeue();

                if (entry == null)
                    return;

                TimeSpan delay = DateTime.Now - entry.Submitted;
                if(delay != showDelay)
                    Debug.WriteLine("Delay: " + delay.ToString("g"));

                await SetPosition(entry.Position, entry.Speed);
            }
        }

        public void EnqueuePosition(byte position, byte speed)
        {
            _queue.Enqueue(new QueueEntry(position, speed));
        }

        public async Task<bool> SetPosition(byte position, byte speed)
        {
            try
            {
                if (!await Initialize())
                    return false;

                IBuffer buffer = GetBuffer(position, speed);

                var result = await _writeCharacteristics.WriteValueAsync(buffer);

                return result == GattCommunicationStatus.Success;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                OnDisconnected(e);
                return false;
            }
        }

        public async Task<bool> Initialize()
        {
            if (_initialized) return true;

            IBuffer buffer = GetBuffer(0);

            GattCommunicationStatus status = await _commandCharacteristics.WriteValueAsync(buffer);

            if (status != GattCommunicationStatus.Success)
                return false;

            _notifyCharacteristics.ValueChanged += NotifyCharacteristicsOnValueChanged;

            _initialized = true;
            return true;
        }

        private void NotifyCharacteristicsOnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            DataReader reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] bytes = new byte[reader.UnconsumedBufferLength];

            reader.ReadBytes(bytes);

            Debug.WriteLine(string.Join("-", bytes.Select(b => b.ToString("X2"))));
        }

        private IBuffer GetBuffer(params byte[] data)
        {
            DataWriter writer = new DataWriter();
            writer.WriteBytes(data);
            return writer.DetachBuffer();
        }

        public static class Uids
        {
            public static Guid MainService = Guid.Parse("88f80580-0000-01e6-aace-0002a5d5c51b");
            public static Guid WriteCharacteristics = Guid.Parse("88f80581-0000-01e6-aace-0002a5d5c51b");
            public static Guid StatusNotificationCharacteristics = Guid.Parse("88f80582-0000-01e6-aace-0002a5d5c51b");
            public static Guid CommandCharacteristics = Guid.Parse("88f80583-0000-01e6-aace-0002a5d5c51b");
        }

        protected virtual void OnDisconnected(Exception e)
        {
            Close();
            Disconnected?.Invoke(this, e);
        }
    }
}