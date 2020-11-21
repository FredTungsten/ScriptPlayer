using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Buttplug.Client;

namespace ScriptPlayer.Shared
{
    public class ButtplugDevice : Device
    {
        private readonly ButtplugClientDevice _device;
        private readonly ButtplugAdapter _buttplugAdapter;

        public uint Index => _device.Index;
        public ButtplugClientDevice Device => _device;

        public ButtplugDevice(ButtplugClientDevice device, ButtplugAdapter buttplugAdapter)
        {
            Name = device.Name;
            _device = device;
            _buttplugAdapter = buttplugAdapter;
        }

        protected override async Task Set(DeviceCommandInformation information)
        {
            try
            {
                await _buttplugAdapter.Set(_device, information);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                OnDisconnected(e);
            }
        }

        public override async Task Set(IntermediateCommandInformation information)
        {
            try
            {
                await _buttplugAdapter.Set(_device, information);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                OnDisconnected(e);
            }
        }

        protected override void StopInternal()
        {
            try
            {
                _buttplugAdapter.Stop(_device);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                OnDisconnected(e);
            }
        }
    }
}