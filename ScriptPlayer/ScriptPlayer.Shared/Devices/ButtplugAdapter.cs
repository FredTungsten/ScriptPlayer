using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Buttplug.Client;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace ScriptPlayer.Shared
{
    public class ButtplugAdapter : DeviceController, INotifyPropertyChanged, IDisposable
    {
        private ButtplugWSClient _client;
        private readonly string _url;
        private readonly List<ButtplugDevice> _devices;

        public ButtplugAdapter(ButtplugConnectionSettings settings)
        {
            _devices = new List<ButtplugDevice>();
            _url = settings.Url;
        }

        private void Client_DeviceAddedOrRemoved(object sender, DeviceEventArgs deviceEventArgs)
        {
            var device = DirtyHacks.GetPrivateField<ButtplugClientDevice>(deviceEventArgs, "device");
            var action = DirtyHacks.GetPrivateField<DeviceEventArgs.DeviceAction>(deviceEventArgs, "action");

            switch (action)
            {
                case DeviceEventArgs.DeviceAction.ADDED:
                    AddDevice(device);
                    break;
                case DeviceEventArgs.DeviceAction.REMOVED:
                    RemoveDevice(device);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task<bool> Connect()
        {
            try
            {
                var client = new ButtplugWSClient("ScriptPlayer");
                client.DeviceAdded += Client_DeviceAddedOrRemoved;
                client.DeviceRemoved += Client_DeviceAddedOrRemoved;

                await client.Connect(new Uri(_url));
                _client = client;
                AddUnknownDevices((await GetDeviceList()).ToList());

                return true;
            }
            catch(Exception)
            {
                _client = null;
                return false;
            }
        }

        private void AddUnknownDevices(List<ButtplugClientDevice> devices)
        {
            List<ButtplugClientDevice> newDevices = new List<ButtplugClientDevice>();
            foreach (var device in devices)
            {
                if(_devices.All(d => d.Index != device.Index))
                    newDevices.Add(device);
            }

            foreach (ButtplugClientDevice device in newDevices)
            {
                AddDevice(device);
            }
        }

        private void RemoveDevice(ButtplugClientDevice device)
        {
            ButtplugDevice localDevice = _devices.SingleOrDefault(dev => dev.Index == device.Index);
            if (localDevice == null) return;

            _devices.Remove(localDevice);
            OnDeviceRemoved(localDevice);
        }

        private void AddDevice(ButtplugClientDevice device)
        {
            var newDevice = new ButtplugDevice(device, this);
            _devices.Add(newDevice);
            OnDeviceFound(newDevice);
        }

        private async Task<IEnumerable<ButtplugClientDevice>> GetDeviceList()
        {
            try
            {
                //TODO Still not working?
                await _client.RequestDeviceList();
                //await Task.Delay(2000);
                return _client.getDevices();
            }
            catch
            {
                return new List<ButtplugClientDevice>();
            }
        }

        public async Task Set(ButtplugClientDevice device, IntermediateCommandInformation information)
        {
            if (_client == null) return;

            if (device.AllowedMessages.Contains(nameof(SingleMotorVibrateCmd)))
            {
                double speedFrom = LaunchToVibrator(information.DeviceInformation.PositionFromOriginal);
                double speedTo = LaunchToVibrator(information.DeviceInformation.PositionToOriginal);

                double speed = Math.Min(1,
                    Math.Max(0, speedFrom * (1 - information.Progress) + speedTo * information.Progress));

                var response = await _client.SendDeviceMessage(device,
                    new SingleMotorVibrateCmd(device.Index, speed));
            }
        }

        public async Task Set(ButtplugClientDevice device, DeviceCommandInformation information)
        {
            if (_client == null) return;

            if (device.AllowedMessages.Contains(nameof(FleshlightLaunchFW12Cmd)))
            {
                var response = await _client.SendDeviceMessage(device, new FleshlightLaunchFW12Cmd(device.Index, information.SpeedTransformed, information.PositionToTransformed));
            }
            else if (device.AllowedMessages.Contains(nameof(KiirooCmd)))
            {
                var response = await _client.SendDeviceMessage(device, new KiirooCmd(device.Index, LaunchToKiiroo(information.PositionToOriginal, 0, 4)));
            }
            else if (device.AllowedMessages.Contains(nameof(SingleMotorVibrateCmd)))
            {
                var response = await _client.SendDeviceMessage(device, new SingleMotorVibrateCmd(device.Index, LaunchToVibrator(information.PositionFromOriginal)));

                /*
#pragma warning disable CS4014
                Task.Run(new Action(async () =>
                {
                    TimeSpan duration = TimeSpan.FromMilliseconds(Math.Min(1000, information.Duration.TotalMilliseconds / 2.0));
                    await Task.Delay(duration);
                    await _client.SendDeviceMessage(device, new SingleMotorVibrateCmd(device.Index, 0));
                }));
#pragma warning restore CS4014
*/
            }
            else if (device.AllowedMessages.Contains(nameof(VorzeA10CycloneCmd)))
            {
                var response = await _client.SendDeviceMessage(device, new VorzeA10CycloneCmd(device.Index, LaunchToVorze(information.SpeedOriginal), information.PositionToOriginal > information.PositionFromOriginal));
            }
            else if (device.AllowedMessages.Contains(nameof(LovenseCmd)))
            {
                //await _client.SendDeviceMessage(device, new LovenseCmd(device.Index, LaunchToLovense(position, speed)));
            }
        }

        public async void Stop(ButtplugClientDevice device)
        {
            if (_client == null) return;

            if (device.AllowedMessages.Contains(nameof(StopDeviceCmd)))
            {
                var response = await _client.SendDeviceMessage(device, new StopDeviceCmd(device.Index));
            }
        }

        private uint LaunchToVorze(byte speed)
        {
            return speed;
        }

        private double LaunchToVibrator(byte position)
        {
            const double max = 1.0;
            const double min = 0.1;

            double speedRelative = 1.0 - ((position + 1) / 100.0);
            double result = min + (max-min) * speedRelative;
            return Math.Min(max, Math.Max(min, result));
        }

        private string LaunchToLovense(byte position, byte speed)
        {
            return "https://github.com/metafetish/lovesense-rs";
        }

        private uint LaunchToKiiroo(byte position, uint min, uint max)
        {
            double pos = position / 0.99;

            uint result = Math.Min(max, Math.Max(min, (uint)Math.Round(pos * (max - min) + min)));

            return result;
        }

        public async Task Disconnect()
        {
            if (_client == null) return;
            _devices.Clear();
            await _client.Disconnect();
        }

        public async Task StartScanning()
        {
            if (_client == null) return;
            await _client.StartScanning();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static string GetButtplugApiVersion()
        {
            string location = typeof(ButtplugWSClient).Assembly.Location;
            if (string.IsNullOrWhiteSpace(location))
                return "?";

            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(location);
            return fileVersionInfo.ProductVersion;
        }

        public override async void ScanForDevices()
        {
            await StartScanning();
        }

        public async void Dispose()
        {
            try
            {
                await Disconnect();
            }
            catch
            {
                
            }
        }
    }

    public class ButtplugConnectionSettings
    {
        public string Url { get; set; }

        public const string DefaultUrl = "ws://localhost:12345/buttplug";
        public ButtplugConnectionSettings()
        {
            Url = DefaultUrl;
        }
    }
}