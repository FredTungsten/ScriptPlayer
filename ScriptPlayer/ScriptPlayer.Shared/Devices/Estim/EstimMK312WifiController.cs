using RexLabsWifiShock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptPlayer.Shared.Devices.Estim
{
    /// <summary>
    /// Device Controller for the MK312 Wifi Interface
    /// 
    /// Uses https://github.com/Rangarig/MK312WIFI
    /// </summary>
    public class EstimMK312WifiController: DeviceController
    {
        private EStimMK312Device _device = null;

        /// <summary>
        /// Returns an MK312 Device, if one is connected to the local wifi
        /// </summary>
        /// <returns>Returns either the Device handle, or null, if no device can be found</returns>
        public EStimMK312Device FindDeviceWifi()
        {
            try
            {
                // If device already open it gets disposed, and opened again
                if (_device != null)
                {
                    _device.Dispose();
                    OnDeviceRemoved(_device);

                    _device = null;
                }
                // Opens the WIFI Communication to the device, and if successful, returns it to the caller
                WifiComm comm = new WifiComm();
                MK312Device device = new MK312Device(comm, false, false);

                device.connect();

                //device.writeToDisplay("ScPlay");
                device.initializeChannels();

                return new EStimMK312Device(device);
            }
            catch (Exception)
            {
            }
            return null;
        }

        /// <summary>
        /// Sets the Device as the Aktive one
        /// </summary>
        /// <param name="device"></param>
        public void SetDevice(EStimMK312Device device)
        {
            _device = device;
            OnDeviceFound(_device);
        }

    }
}
