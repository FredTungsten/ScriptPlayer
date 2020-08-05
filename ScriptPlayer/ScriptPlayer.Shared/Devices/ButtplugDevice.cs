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
            await _buttplugAdapter.Set(_device, information);
        }

        public override async Task Set(IntermediateCommandInformation information)
        {
            await _buttplugAdapter.Set(_device, information);
        }

        protected override void StopInternal()
        {
            _buttplugAdapter.Stop(_device);
        }
    }
}