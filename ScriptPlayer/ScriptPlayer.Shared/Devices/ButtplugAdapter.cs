using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Buttplug;
using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;
using JetBrains.Annotations;
using ScriptPlayer.Shared.Helpers;

namespace ScriptPlayer.Shared
{
    public class ButtplugAdapter : DeviceController, INotifyPropertyChanged, IDisposable
    {
        private ButtplugClient _client;
        private readonly SemaphoreSlim _clientLock = new SemaphoreSlim(1);
        private readonly string _url;
        private readonly ConcurrentDictionary<uint, ButtplugDevice> _devices;

        public VibratorConversionMode VibratorConversionMode { get; set; } = VibratorConversionMode.PositionToSpeed;

        public ButtplugAdapter(ButtplugConnectionSettings settings)
        {
            _devices = new ConcurrentDictionary<uint, ButtplugDevice>();
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

        private void RecordButtplugException(string method, Exception e)
        {
            Debug.WriteLine($"Exception in {method}: {e.Message}");
            File.AppendAllText(
                Environment.ExpandEnvironmentVariables("%APPDATA%\\ScriptPlayer\\ButtplugConnectionError.log"),
                ExceptionHelper.BuildException(e));
        }

        public async Task<bool> Connect()
        {
            if (_client?.Connected ?? false)
            {
                return true;
            }

            await Disconnect();

            try
            {
                var client = new ButtplugClient("ScriptPlayer");
                client.DeviceAdded += Client_DeviceAdded;
                client.DeviceRemoved += Client_DeviceRemoved;
                client.ErrorReceived += Client_ErrorReceived;
                client.PingTimeout += Client_PingTimeout;
                client.ScanningFinished += Client_ScanningFinished;
                client.ServerDisconnect += Client_ServerDisconnect;

                _client = client;

                await client.ConnectAsync(new ButtplugWebsocketConnectorOptions(new Uri(_url)));
                
                foreach (var buttplugClientDevice in _client.Devices)
                {
                    AddDevice(buttplugClientDevice);
                }

                return true;
            }
            catch (Exception e)
            {
                RecordButtplugException("ButtplugAdapter.Connect", e);
                _client.Dispose();
                _client = null;
                return false;
            }
        }

        private void Client_ServerDisconnect(object sender, EventArgs e)
        {
            _client?.Dispose();
            _client = null;
            foreach (var device in _devices.Values)
            {
                OnDeviceRemoved(device);
            }
            _devices.Clear();
            OnDisconnected();
        }

        private void Client_ScanningFinished(object sender, EventArgs e)
        {
            //TODO
        }

        private void Client_PingTimeout(object sender, EventArgs e)
        {
            //TODO
        }

        private void Client_ErrorReceived(object sender, ButtplugExceptionEventArgs buttplugExceptionEventArgs)
        {
            RecordButtplugException("ButtplugAdapter.Client_ErrorReceived", buttplugExceptionEventArgs.Exception);
        }

        private void RemoveDevice(ButtplugDevice device)
        {
            if (device == null) return;

            if (_devices.TryRemove(device.Index, out var old))
            {
                OnDeviceRemoved(old);
            }
        }

        private void RemoveDevice(ButtplugClientDevice device)
        {
            if(_devices.TryGetValue(device.Index, out var localDevice)) {
                RemoveDevice(localDevice);
            }
        }

        private void AddDevice(ButtplugClientDevice device)
        {
            var newDevice = new ButtplugDevice(device, this);
            _devices.AddOrUpdate(newDevice.Index,
                (newIdx) =>
                {
                    OnDeviceFound(newDevice);
                    return newDevice;
                },
                (updateIdx, oldDev) =>
                {
                    OnDeviceRemoved(oldDev);
                    OnDeviceFound(newDevice);
                    return newDevice;
                });
        }

        public async Task Set(ButtplugClientDevice device, IntermediateCommandInformation information)
        {
            if (_client == null) return;

            if (device.AllowedMessages.ContainsKey(ServerMessage.Types.MessageAttributeType.VibrateCmd))
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
                    case VibratorConversionMode.PositionToSpeedInverted:
                        {
                            double speedFrom = CommandConverter.LaunchPositionToVibratorSpeed((byte) (99 - information.DeviceInformation.PositionFromOriginal));
                            double speedTo = CommandConverter.LaunchPositionToVibratorSpeed((byte) (99 - information.DeviceInformation.PositionToOriginal));

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
                    case VibratorConversionMode.SpeedTimesLengthFullDuration:
                    {
                        speed = CommandConverter.LaunchSpeedAndLengthToVibratorSpeed(
                            information.DeviceInformation.SpeedOriginal,
                            information.DeviceInformation.PositionFromTransformed,
                            information.DeviceInformation.PositionToTransformed);
                        speed = information.DeviceInformation.TransformSpeed(speed);
                            break;
                    }
                    case VibratorConversionMode.SpeedTimesLengthHalfDuration:
                    {
                        if (information.Progress < 0.5)
                        {
                            speed = CommandConverter.LaunchSpeedAndLengthToVibratorSpeed(
                                information.DeviceInformation.SpeedOriginal,
                                information.DeviceInformation.PositionFromTransformed,
                                information.DeviceInformation.PositionToTransformed);
                            speed = information.DeviceInformation.TransformSpeed(speed);
                            }
                        else
                        {
                            speed = 0.0;
                        }

                        break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                try
                {
                    await _clientLock.WaitAsync();

                    await device.SendVibrateCmd(speed);

                    //ButtplugMessage response = await device.SendMessageAsync(new SingleMotorVibrateCmd(device.Index, speed));

                    //await CheckResponse(response);
                }
                catch (Exception e)
                {
                    RecordButtplugException("ButtplugAdapter.Set(bcd, ici)", e);
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

                if (device.AllowedMessages.ContainsKey(ServerMessage.Types.MessageAttributeType.LinearCmd))
                {
                    double val = information.PositionToTransformed / (double)99;
                    val = Math.Min(1.0, Math.Max(0, val));

                    await device.SendLinearCmd(
                        (uint) information.DurationStretched.TotalMilliseconds, val);
                }
                else if (device.AllowedMessages.ContainsKey(ServerMessage.Types.MessageAttributeType.VibrateCmd))
                {
                    switch (VibratorConversionMode)
                    {
                        case VibratorConversionMode.PositionToSpeed:
                            await device.SendVibrateCmd(information.TransformSpeed(CommandConverter.LaunchPositionToVibratorSpeed(information.PositionFromOriginal)));
                            break;
                        case VibratorConversionMode.PositionToSpeedInverted:
                            await device.SendVibrateCmd(information.TransformSpeed(CommandConverter.LaunchPositionToVibratorSpeed((byte)(99 - information.PositionFromOriginal))));
                            break;
                        case VibratorConversionMode.SpeedHalfDuration:
                        case VibratorConversionMode.SpeedFullDuration:
                            await device.SendVibrateCmd(information.TransformSpeed(CommandConverter.LaunchSpeedToVibratorSpeed(information.SpeedTransformed)));
                            break;
                        case VibratorConversionMode.SpeedTimesLengthHalfDuration:
                        case VibratorConversionMode.SpeedTimesLengthFullDuration:
                            await device.SendVibrateCmd(information.TransformSpeed(CommandConverter.LaunchSpeedAndLengthToVibratorSpeed(
                                information.SpeedOriginal,
                                information.PositionFromTransformed,
                                information.PositionToTransformed)));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (device.AllowedMessages.ContainsKey(ServerMessage.Types.MessageAttributeType.RotateCmd))
                {
                    await device.SendRotateCmd(CommandConverter.LaunchToVorzeSpeed(information), information.PositionToTransformed > information.PositionFromTransformed);
                }
            }
            catch (Exception e)
            {
                RecordButtplugException("ButtplugAdapter.Set(bcd, dci)", e);
            }
            finally
            {
                _clientLock.Release();
            }
        }

        public async void Stop(ButtplugClientDevice device)
        {
            if (_client == null)
                return;

            if (!device.AllowedMessages.ContainsKey(ServerMessage.Types.MessageAttributeType.StopDeviceCmd))
                return;

            try
            {
                await device.SendStopDeviceCmd();
            }
            catch (Exception e)
            {
                RecordButtplugException("ButtplugAdapter.Stop", e);
            }
        }

        public async Task Disconnect()
        {
            try
            {
                if (_client?.Connected ?? false)
                    await _client.DisconnectAsync();
                
                _client?.Dispose();
            }
            catch (Exception e)
            {
                RecordButtplugException("ButtplugAdapter.Disconnect", e);
            }
        }

        public async Task StartScanning()
        {
            if (_client == null) return;
            try
            {
                await _client.StartScanningAsync();
            }
            catch (Exception e)
            {
                RecordButtplugException("ButtplugAdapter.StartScanning", e);
            }
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
            return "https://github.com/intiface/intiface-desktop/releases/";
            //return "https://github.com/buttplugio/buttplug-windows-suite/releases/tag/" + GetButtplugApiVersion();
        }

        public async void ScanForDevices()
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
                RecordButtplugException("ButtplugAdapter.Dispose", e);
            }
        }
    }

    public enum VibratorConversionMode
    {
        PositionToSpeed,
        PositionToSpeedInverted,
        SpeedHalfDuration,
        SpeedFullDuration,
        SpeedTimesLengthFullDuration,
        SpeedTimesLengthHalfDuration,
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