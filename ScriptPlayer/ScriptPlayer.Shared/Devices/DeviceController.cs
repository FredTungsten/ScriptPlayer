using System;

namespace ScriptPlayer.Shared
{
    public abstract class DeviceController
    {
        public event EventHandler<Device> DeviceFound;
        public abstract void ScanForDevices();

        protected virtual void OnDeviceFound(Device e)
        {
            DeviceFound?.Invoke(this, e);
        }
    }
}