using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace ScriptPlayer.Shared.Devices
{
    public class EStimAudioController : DeviceController
    {
        private EStimAudioDevice _device = null;

        public List<DirectSoundDeviceInfo> GetAudioDevices()
        {
            return DirectSoundOut.Devices.ToList();
        }

        public void SetDevice(DirectSoundDeviceInfo device, EstimParameters parameters)
        {
            if (_device != null)
            {
                _device.Dispose();
                OnDeviceRemoved(_device);
            }

            _device = new EStimAudioDevice(device, parameters);
            OnDeviceFound(_device);
        }
    }
}
