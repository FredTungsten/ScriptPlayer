using System;
using ScriptPlayer.Shared.Interfaces;

namespace ScriptPlayer.Shared
{
    public abstract class DeviceController
    {
        public event EventHandler Disconnected;

        public event EventHandler<IDevice> DeviceFound;

        public event EventHandler<IDevice> DeviceRemoved;

        protected virtual void OnDeviceFound(IDevice e)
        {
            DeviceFound?.Invoke(this, e);
        }

        protected virtual void OnDeviceRemoved(IDevice e)
        {
            DeviceRemoved?.Invoke(this, e);
        }

        protected virtual void OnDisconnected()
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}