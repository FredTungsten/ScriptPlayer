using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Client;
using Buttplug.Core.Messages;

namespace ScriptPlayer
{
    public interface IDeviceController
    {
        void Set(byte speed, byte position);
    }

    public class ButtplugAdapter : IDeviceController
    {
        private readonly ButtplugWSClient _client;
        private readonly string _url;

        private List<ButtplugClientDevice> _devices;

        public ButtplugAdapter(string url = "ws://localhost:12345/buttplug")
        {
            _devices = new List<ButtplugClientDevice>();

            _url = url;
            _client = new ButtplugWSClient("ScriptPlayer");
            _client.DeviceAdded += Client_DeviceAddedOrRemoved;
            _client.DeviceRemoved += Client_DeviceAddedOrRemoved;
        }

        private void Client_DeviceAddedOrRemoved(object sender, DeviceEventArgs deviceEventArgs)
        {
            var device = GetPrivateField<ButtplugClientDevice>(deviceEventArgs, "device");
            var action = GetPrivateField<DeviceEventArgs.DeviceAction>(deviceEventArgs, "action");

            switch (action)
            {
                case DeviceEventArgs.DeviceAction.ADDED:
                    _devices.Add(device);
                    break;
                case DeviceEventArgs.DeviceAction.REMOVED:
                    _devices.RemoveAll(dev => dev.Index == device.Index);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private T GetPrivateField<T>(object obj, string fieldName)
        {
            return (T)obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
        }

        public async Task<bool> Connect()
        {
            await _client.Connect(new Uri(_url));
            _devices = (await GetDeviceList()).ToList();

            return true;
        }

        private async Task<IEnumerable<ButtplugClientDevice>> GetDeviceList()
        {
            try
            {
                //await _client.RequestDeviceList();
                //devices = _client.getDevices();
            }
            catch
            {

            }

            return new List<ButtplugClientDevice>();
        }

        public async void Set(byte speed, byte position)
        {
            var devices = _devices;
            foreach (ButtplugClientDevice device in devices)
            {
                if (device.AllowedMessages.Contains(nameof(FleshlightLaunchFW12Cmd)))
                {
                    await _client.SendDeviceMessage(device, new FleshlightLaunchFW12Cmd(device.Index, speed, position));
                }
                else if (device.AllowedMessages.Contains(nameof(KiirooCmd)))
                {
                    await _client.SendDeviceMessage(device, new KiirooCmd(device.Index, LaunchToKiiroo(position, 0, 4)));
                }
                else if (device.AllowedMessages.Contains(nameof(SingleMotorVibrateCmd)))
                {
                    await _client.SendDeviceMessage(device, new SingleMotorVibrateCmd(device.Index, LaunchToVibrator(speed)));
                }
                else if (device.AllowedMessages.Contains(nameof(VorzeA10CycloneCmd)))
                {
                    await _client.SendDeviceMessage(device, new VorzeA10CycloneCmd(device.Index, LaunchToVorze(speed), true));
                }
                else if (device.AllowedMessages.Contains(nameof(LovenseCmd)))
                {
                    //await _client.SendDeviceMessage(device, new LovenseCmd(device.Index, LaunchToLovense(position, speed)));
                }
            }
        }

        private uint LaunchToVorze(byte speed)
        {
            return speed;
        }

        private double LaunchToVibrator(byte speed)
        {
            return speed / 99.0;
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
    }
}
