using System;

namespace ScriptPlayer.Shared
{
    public abstract class DeviceController
    {
        public event EventHandler<Device> DeviceFound;

        public event EventHandler<Device> DeviceRemoved;
        public abstract void ScanForDevices();

        protected virtual void OnDeviceFound(Device e)
        {
            DeviceFound?.Invoke(this, e);
        }

        protected virtual void OnDeviceRemoved(Device e)
        {
            DeviceRemoved?.Invoke(this, e);
        }
    }
}