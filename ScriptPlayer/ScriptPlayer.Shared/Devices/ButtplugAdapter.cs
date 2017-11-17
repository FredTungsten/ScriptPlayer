using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Client;
using Buttplug.Core;
using Buttplug.Core.Messages;
using JetBrains.Annotations;
using ScriptPlayer.Shared.Helpers;

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
            ButtplugClientDevice device = deviceEventArgs.Device; // DirtyHacks.GetAnythingWithThatName<ButtplugClientDevice>(deviceEventArgs, "device");
            DeviceEventArgs.DeviceAction action = deviceEventArgs.Action; // DirtyHacks.GetAnythingWithThatName<DeviceEventArgs.DeviceAction>(deviceEventArgs, "action");

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
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                File.AppendAllText(Environment.ExpandEnvironmentVariables("%APPDATA%\\ScriptPlayer\\ButtplugConnectionError.log"), ExceptionHelper.BuildException(e));
                _client = null;
                return false;
            }
        }

        private void AddUnknownDevices(List<ButtplugClientDevice> devices)
        {
            List<ButtplugClientDevice> newDevices = devices.Where(device => _devices.All(d => d.Index != device.Index)).ToList();

            foreach (ButtplugClientDevice device in newDevices)
            {
                AddDevice(device);
            }
        }

        private void RemoveDevice(ButtplugDevice device)
        {
            if (device == null) return;

            _devices.Remove(device);
            OnDeviceRemoved(device);
        }

        private void RemoveDevice(ButtplugClientDevice device)
        {
            ButtplugDevice localDevice = _devices.SingleOrDefault(dev => dev.Index == device.Index);
            RemoveDevice(localDevice);
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
                await _client.RequestDeviceList();
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

                ButtplugMessage response = await _client.SendDeviceMessage(device,
                    new SingleMotorVibrateCmd(device.Index, speed));

                await CheckResponse(response);
            }
        }

        public async Task Set(ButtplugClientDevice device, DeviceCommandInformation information)
        {
            if (_client == null) return;

            ButtplugMessage response = null;

            if (device.AllowedMessages.Contains(nameof(FleshlightLaunchFW12Cmd)))
            {
                response = await _client.SendDeviceMessage(device, new FleshlightLaunchFW12Cmd(device.Index, information.SpeedTransformed, information.PositionToTransformed));
            }
            else if (device.AllowedMessages.Contains(nameof(KiirooCmd)))
            {
                response = await _client.SendDeviceMessage(device, new KiirooCmd(device.Index, LaunchToKiiroo(information.PositionToOriginal, 0, 4)));
            }
            else if (device.AllowedMessages.Contains(nameof(SingleMotorVibrateCmd)))
            {
                response = await _client.SendDeviceMessage(device, new SingleMotorVibrateCmd(device.Index, LaunchToVibrator(information.PositionFromOriginal)));
            }
            else if (device.AllowedMessages.Contains(nameof(VorzeA10CycloneCmd)))
            {
                response = await _client.SendDeviceMessage(device, new VorzeA10CycloneCmd(device.Index, LaunchToVorze(information.SpeedOriginal), information.PositionToOriginal > information.PositionFromOriginal));
            }
            else if (device.AllowedMessages.Contains(nameof(LovenseCmd)))
            {
                return;
                //await _client.SendDeviceMessage(device, new LovenseCmd(device.Index, LaunchToLovense(position, speed)));
            }

            await CheckResponse(response);
        }

        /*
        private string LaunchToLovense(byte position, byte speed)
        {
            return "https://github.com/metafetish/lovesense-rs";
        }
        */

        private async Task CheckResponse(ButtplugMessage response)
        {
            if (response is Error error)
            {
                if (error.ErrorCode == Error.ErrorClass.ERROR_UNKNOWN)
                    await Disconnect();
            }
        }

        public async void Stop(ButtplugClientDevice device)
        {
            if (_client == null) return;

            if (!device.AllowedMessages.Contains(nameof(StopDeviceCmd))) return;

            ButtplugMessage response = await _client.SendDeviceMessage(device, new StopDeviceCmd(device.Index));
            await CheckResponse(response);
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

        private uint LaunchToKiiroo(byte position, uint min, uint max)
        {
            double pos = position / 0.99;

            uint result = Math.Min(max, Math.Max(min, (uint)Math.Round(pos * (max - min) + min)));

            return result;
        }

        public async Task Disconnect()
        {
            try
            {
                if (_client == null) return;
                foreach (var device in _devices.ToList())
                {
                    RemoveDevice(device);
                }
                await _client.Disconnect();
            }
            finally
            {
                OnDisconnected();
            }
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
            catch (Exception e)
            {
                Debug.WriteLine("Exception in ButtplugAdapter.Dispose: " + e.Message);
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