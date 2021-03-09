using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Buttplug;

namespace ScriptPlayer.Shared
{
    public class ButtplugDevice : Device
    {
        private readonly ButtplugAdapter _buttplugAdapter;

        public uint Index => Device.Index;
        public ButtplugClientDevice Device { get; }

        public ButtplugDevice(ButtplugClientDevice device, ButtplugAdapter buttplugAdapter)
        {
            Name = device.Name;
            Device = device;
            _buttplugAdapter = buttplugAdapter;
        }

        protected override async Task Set(DeviceCommandInformation information)
        {
            try
            {
                await _buttplugAdapter.Set(Device, information);
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
                await _buttplugAdapter.Set(Device, information);
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
                _buttplugAdapter.Stop(Device);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                OnDisconnected(e);
            }
        }
    }
}