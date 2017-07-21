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
    public class ButtplugAdapter : IDeviceController, INotifyPropertyChanged
    {
        public List<ButtplugClientDevice> Devices
        {
            get { return _devices.ToList(); }
            private set
            {
                if (_devices == value)
                    return;
                _devices = value;
                OnPropertyChanged();
            }
        }

        public const string DefaultUrl = "ws://localhost:12345/buttplug";

        public event EventHandler<string> DeviceAdded;

        private ButtplugWSClient _client;
        private readonly string _url;

        private List<ButtplugClientDevice> _devices;

        public ButtplugAdapter(string url = DefaultUrl)
        {
            Devices = new List<ButtplugClientDevice>();
            _url = url;
        }

        private void Client_DeviceAddedOrRemoved(object sender, DeviceEventArgs deviceEventArgs)
        {
            var device = DirtyHacks.GetPrivateField<ButtplugClientDevice>(deviceEventArgs, "device");
            var action = DirtyHacks.GetPrivateField<DeviceEventArgs.DeviceAction>(deviceEventArgs, "action");

            switch (action)
            {
                case DeviceEventArgs.DeviceAction.ADDED:
                    _devices.Add(device);
                    OnPropertyChanged(nameof(Devices));
                    OnDeviceAdded(device.Name);
                    break;
                case DeviceEventArgs.DeviceAction.REMOVED:
                    _devices.RemoveAll(dev => dev.Index == device.Index);
                    OnPropertyChanged(nameof(Devices));
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
                Devices = (await GetDeviceList()).ToList();

                return true;
            }
            catch(Exception e)
            {
                _client = null;
                return false;
            }
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

        public async void Set(DeviceCommandInformation information)
        {
            if (_client == null) return;

            var devices = _devices.ToList();
            foreach (ButtplugClientDevice device in devices)
            {
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
                    var response = await _client.SendDeviceMessage(device, new SingleMotorVibrateCmd(device.Index, LaunchToVibrator(information.SpeedOriginal)));

#pragma warning disable CS4014
                    Task.Run(new Action(async () =>
                    {
                        TimeSpan duration = TimeSpan.FromMilliseconds(Math.Min(1000, information.Duration.TotalMilliseconds / 2.0));
                        await Task.Delay(duration);
                        await _client.SendDeviceMessage(device, new SingleMotorVibrateCmd(device.Index, 0));
                    }));
#pragma warning restore CS4014
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
        }

        public async void Stop()
        {
            if (_client == null) return;

            var devices = _devices.ToList();
            foreach (ButtplugClientDevice device in devices)
            {
                if (device.AllowedMessages.Contains(nameof(StopDeviceCmd)))
                {
                    var response = await _client.SendDeviceMessage(device, new StopDeviceCmd(device.Index));
                }
            }
        }

        private uint LaunchToVorze(byte speed)
        {
            return speed;
        }

        private double LaunchToVibrator(byte speed)
        {
            double speedRelative = (speed+1) / 100.0;
            double result = 0.25 + 0.75 * speedRelative;
            return Math.Min(1.0, Math.Max(0.25, result));
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
            OnPropertyChanged(nameof(Devices));
            await _client.Disconnect();
        }

        public async Task StartScanning()
        {
            if (_client == null) return;
            await _client.StartScanning();
        }

        protected virtual void OnDeviceAdded(string e)
        {
            DeviceAdded?.Invoke(this, e);
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
    }
}