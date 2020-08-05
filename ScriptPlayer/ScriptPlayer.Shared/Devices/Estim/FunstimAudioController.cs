using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace ScriptPlayer.Shared.Devices
{
    public class FunstimAudioController : DeviceController
    {
        private FunstimAudioDevice _device = null;

        public List<DirectSoundDeviceInfo> GetAudioDevices()
        {
            return DirectSoundOut.Devices.ToList();
        }

        public void SetDevice(DirectSoundDeviceInfo device, FunstimParameters parameters)
        {
            if (_device != null)
            {
                _device.Dispose();
                OnDeviceRemoved(_device);
            }

            _device = new FunstimAudioDevice(device, parameters);
            OnDeviceFound(_device);
        }
    }
}
