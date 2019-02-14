using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
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
        private readonly SemaphoreSlim _clientLock = new SemaphoreSlim(1);
        private readonly string _url;
        private readonly List<ButtplugDevice> _devices;

        public ButtplugAdapter(ButtplugConnectionSettings settings)
        {
            _devices = new List<ButtplugDevice>();
            _url = settings.Url;
        }

        private void Client_DeviceAddedOrRemoved(object sender, DeviceEventArgs deviceEventArgs)
        {
            ButtplugClientDevice device = deviceEventArgs.Device; 
            DeviceEventArgs.DeviceAction action = deviceEventArgs.Action;

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

                return true;
            }
            catch (Exception e)
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

        public async Task Set(ButtplugClientDevice device, IntermediateCommandInformation information)
        {
            if (_client == null) return;

            if (device.AllowedMessages.ContainsKey(nameof(SingleMotorVibrateCmd)))
            {
                double speedFrom = CommandConverter.LaunchToVibrator(information.DeviceInformation.PositionFromOriginal);
                double speedTo = CommandConverter.LaunchToVibrator(information.DeviceInformation.PositionToOriginal);

                double speed = speedFrom * (1 - information.Progress) + speedTo * information.Progress * information.DeviceInformation.SpeedMultiplier;
                speed = information.DeviceInformation.TransformSpeed(speed);

                try
                {
                    await _clientLock.WaitAsync();

                    ButtplugMessage response = await _client.SendDeviceMessage(device,
                        new SingleMotorVibrateCmd(device.Index, speed));

                    await CheckResponse(response);
                }
                finally
                {
                    _clientLock.Release();
                }
            }
        }

        public async Task Set(ButtplugClientDevice device, DeviceCommandInformation information)
        {
            if (_client == null) return;

            try
            {
                await _clientLock.WaitAsync();

                ButtplugDeviceMessage message = null;

                if (device.AllowedMessages.ContainsKey(nameof(FleshlightLaunchFW12Cmd)))
                {
                    message = new FleshlightLaunchFW12Cmd(device.Index, information.SpeedTransformed, information.PositionToTransformed);
                }
                else if (device.AllowedMessages.ContainsKey(nameof(KiirooCmd)))
                {
                    message = new KiirooCmd(device.Index, CommandConverter.LaunchToKiiroo(information.PositionToOriginal, 0, 4));
                }
                /*else if (device.AllowedMessages.ContainsKey(nameof(VibrateCmd)))
                {
                    message = new VibrateCmd(device.Index, new List<VibrateCmd.VibrateSubcommand>{new VibrateCmd.VibrateSubcommand(0, LaunchToVibrator(information.PositionFromOriginal))});
                }*/
                else if (device.AllowedMessages.ContainsKey(nameof(SingleMotorVibrateCmd)))
                {
                    message = new SingleMotorVibrateCmd(device.Index, information.TransformSpeed(CommandConverter.LaunchToVibrator(information.PositionFromOriginal)));
                }
                else if (device.AllowedMessages.ContainsKey(nameof(VorzeA10CycloneCmd)))
                {
                    // position is used as speed here
                    message = new VorzeA10CycloneCmd(device.Index, CommandConverter.SimpleVorzeSpeed(information), information.PositionFromOriginal > 50);
                }
                else if (device.AllowedMessages.ContainsKey(nameof(LovenseCmd)))
                {
                    //message = new LovenseCmd(device.Index, LaunchToLovense(position, speed));
                }

                if (message == null) return;

                ButtplugMessage response = await _client.SendDeviceMessage(device, message);
                await CheckResponse(response);
            }
            finally
            {
                _clientLock.Release();
            }
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
            else
            {
                //Console.WriteLine(response.Id);
            }
        }

        public async void Stop(ButtplugClientDevice device)
        {
            if (_client == null) return;

            if (!device.AllowedMessages.ContainsKey(nameof(StopDeviceCmd))) return;

            ButtplugMessage response = await _client.SendDeviceMessage(device, new StopDeviceCmd(device.Index));
            await CheckResponse(response);
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