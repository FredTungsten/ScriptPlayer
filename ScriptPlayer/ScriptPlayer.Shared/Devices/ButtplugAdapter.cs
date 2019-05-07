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
using Buttplug.Client.Connectors.WebsocketConnector;
using Buttplug.Core;
using Buttplug.Core.Messages;
using JetBrains.Annotations;
using ScriptPlayer.Shared.Helpers;

namespace ScriptPlayer.Shared
{
    public class ButtplugAdapter : DeviceController, INotifyPropertyChanged, IDisposable
    {
        private ButtplugClient _client;
        private readonly SemaphoreSlim _clientLock = new SemaphoreSlim(1);
        private readonly string _url;
        private readonly List<ButtplugDevice> _devices;

        public VibratorConversionMode VibratorConversionMode { get; set; } = VibratorConversionMode.PositionToSpeed;

        public ButtplugAdapter(ButtplugConnectionSettings settings)
        {
            _devices = new List<ButtplugDevice>();
            _url = settings.Url;
        }

        private void Client_DeviceAdded(object sender, DeviceAddedEventArgs deviceEventArgs)
        {
            AddDevice(deviceEventArgs.Device);
        }


        private void Client_DeviceRemoved(object sender, DeviceRemovedEventArgs deviceEventArgs)
        {
            RemoveDevice(deviceEventArgs.Device);
        }

        public async Task<bool> Connect()
        {
            try
            {
                var client = new ButtplugClient("ScriptPlayer", new ButtplugWebsocketConnector(new Uri(_url)));
                client.DeviceAdded += Client_DeviceAdded;
                client.DeviceRemoved += Client_DeviceRemoved;
                client.ErrorReceived += Client_ErrorReceived;
                client.Log += Client_Log;
                client.PingTimeout += Client_PingTimeout;
                client.ScanningFinished += Client_ScanningFinished;
                client.ServerDisconnect += Client_ServerDisconnect;

                await client.ConnectAsync();
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

        private void Client_ServerDisconnect(object sender, EventArgs e)
        {
            //TODO
        }

        private void Client_ScanningFinished(object sender, EventArgs e)
        {
            //TODO
        }

        private void Client_PingTimeout(object sender, EventArgs e)
        {
            //TODO
        }

        private void Client_Log(object sender, LogEventArgs logEventArgs)
        {
            //TODO
        }

        private void Client_ErrorReceived(object sender, ButtplugExceptionEventArgs buttplugExceptionEventArgs)
        {
            //TODO
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

            if (device.AllowedMessages.ContainsKey(typeof(SingleMotorVibrateCmd)))
            {
                double speed;
                switch (VibratorConversionMode)
                {
                    case VibratorConversionMode.PositionToSpeed:
                    {
                        double speedFrom = CommandConverter.LaunchPositionToVibratorSpeed(information.DeviceInformation.PositionFromOriginal);
                        double speedTo = CommandConverter.LaunchPositionToVibratorSpeed(information.DeviceInformation.PositionToOriginal);

                        speed = speedFrom * (1 - information.Progress) + speedTo * information.Progress * information.DeviceInformation.SpeedMultiplier;
                        speed = information.DeviceInformation.TransformSpeed(speed);

                        break;
                    }
                    case VibratorConversionMode.SpeedHalfDuration:
                    {
                        if (information.Progress < 0.5)
                        {
                            speed = CommandConverter.LaunchSpeedToVibratorSpeed(information.DeviceInformation.SpeedTransformed);
                        }
                        else
                        {
                            speed = 0.0;
                        }

                        break;
                    }
                    case VibratorConversionMode.SpeedFullDuration:
                    {
                        speed = CommandConverter.LaunchSpeedToVibratorSpeed(information.DeviceInformation.SpeedTransformed);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                try
                {
                    await _clientLock.WaitAsync();

                    await device.SendMessageAsync(new SingleMotorVibrateCmd(device.Index, speed));

                    //ButtplugMessage response = await device.SendMessageAsync(new SingleMotorVibrateCmd(device.Index, speed));

                    //await CheckResponse(response);
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

                if (device.AllowedMessages.ContainsKey(typeof(FleshlightLaunchFW12Cmd)))
                {
                    message = new FleshlightLaunchFW12Cmd(device.Index, information.SpeedTransformed, information.PositionToTransformed);
                }
                else if (device.AllowedMessages.ContainsKey(typeof(KiirooCmd)))
                {
                    message = new KiirooCmd(device.Index, CommandConverter.LaunchToKiiroo(information.PositionToOriginal, 0, 4));
                }
                /*else if (device.AllowedMessages.ContainsKey(nameof(VibrateCmd)))
                {
                    message = new VibrateCmd(device.Index, new List<VibrateCmd.VibrateSubcommand>{new VibrateCmd.VibrateSubcommand(0, LaunchPositionToVibratorSpeed(information.PositionFromOriginal))});
                }*/
                else if (device.AllowedMessages.ContainsKey(typeof(SingleMotorVibrateCmd)))
                {
                    switch(VibratorConversionMode)
                    {
                        case VibratorConversionMode.PositionToSpeed:
                            message = new SingleMotorVibrateCmd(device.Index, information.TransformSpeed(CommandConverter.LaunchPositionToVibratorSpeed(information.PositionFromOriginal)));
                            break;
                        case VibratorConversionMode.SpeedHalfDuration:
                        case VibratorConversionMode.SpeedFullDuration:
                            message = new SingleMotorVibrateCmd(device.Index, information.TransformSpeed(CommandConverter.LaunchSpeedToVibratorSpeed(information.SpeedTransformed)));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (device.AllowedMessages.ContainsKey(typeof(VorzeA10CycloneCmd)))
                {
                    message = new VorzeA10CycloneCmd(device.Index, CommandConverter.LaunchToVorzeSpeed(information), information.PositionToTransformed > information.PositionFromTransformed);
                }
                else if (device.AllowedMessages.ContainsKey(typeof(LovenseCmd)))
                {
                    //message = new LovenseCmd(device.Index, LaunchToLovense(position, speed));
                }

                if (message == null) return;

                await device.SendMessageAsync(message);
                //ButtplugMessage response = await _client.SendDeviceMessage(device, message);
                //await CheckResponse(response);
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

            if (!device.AllowedMessages.ContainsKey(typeof(StopDeviceCmd))) return;

            await device.StopDeviceCmd();
            //ButtplugMessage response = await _client.SendDeviceMessage(device, new StopDeviceCmd(device.Index));
            //await CheckResponse(response);
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

                await _client.DisconnectAsync();
            }
            finally
            {
                OnDisconnected();
            }
        }

        public async Task StartScanning()
        {
            if (_client == null) return;
            await _client.StartScanningAsync();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static string GetButtplugApiVersion()
        {
            string location = typeof(ButtplugClient).Assembly.Location;
            if (string.IsNullOrWhiteSpace(location))
                return "?";

            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(location);
            return fileVersionInfo.ProductVersion;
        }

        public static string GetDownloadUrl()
        {
            return "https://github.com/intiface/intiface-desktop/releases/tag/v13.0.0";
            //return "https://github.com/buttplugio/buttplug-windows-suite/releases/tag/" + GetButtplugApiVersion();
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

    public enum VibratorConversionMode
    {
        PositionToSpeed,
        SpeedHalfDuration,
        SpeedFullDuration
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