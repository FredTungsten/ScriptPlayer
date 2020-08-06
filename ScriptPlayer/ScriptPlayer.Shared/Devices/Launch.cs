using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace ScriptPlayer.Shared
{
    public class Launch : Device
    {
        public bool SendCommandsWithResponse { get; set; } = false;

        // Just to make sure it doesn't get disposed or something like that
        // ReSharper disable once NotAccessedField.Local
        private BluetoothLEDevice _device;

        private readonly GattCharacteristic _commandCharacteristics;
        private readonly GattCharacteristic _notifyCharacteristics;
        private readonly GattCharacteristic _writeCharacteristics;

        private bool _initialized;
        

        public Launch(BluetoothLEDevice device, GattCharacteristic writeCharacteristics, GattCharacteristic notifyCharacteristics, GattCharacteristic commandCharacteristics)
        {
            _device = device;
            _writeCharacteristics = writeCharacteristics;
            _notifyCharacteristics = notifyCharacteristics;
            _commandCharacteristics = commandCharacteristics;

            Name = "Fleshlight Launch";
        }

        public async Task<bool> SetPosition(byte position, byte speed)
        {
            try
            {
                if (!await Initialize())
                    return false;

                GattWriteOption option = SendCommandsWithResponse
                    ? GattWriteOption.WriteWithResponse
                    : GattWriteOption.WriteWithoutResponse;

                IBuffer buffer = GetBuffer(position, speed);
                GattCommunicationStatus result = await _writeCharacteristics.WriteValueAsync(buffer, option);

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

        protected override async Task Set(DeviceCommandInformation information)
        {
            await SetPosition(information.PositionToTransformed, information.SpeedTransformed);
        }

        public override Task Set(IntermediateCommandInformation information)
        {
            return Task.CompletedTask;
            // Does not apply
        }

        protected override void StopInternal()
        {
            // Not available
        }

        public override  void Dispose()
        {
            base.Dispose();
            _device?.Dispose();
            _device = null;
        }
    }
}