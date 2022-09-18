using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace ScriptPlayer.Shared
{
    public class LaunchBluetooth : DeviceController
    {
        private bool connectionInProgress;

        public BluetoothLEAdvertisementWatcher BleWatcher { get; set; }
        
        public void Start()
        {
            Debug.WriteLine("Start watching for BLE devices ...");
            BleWatcher.Start();
        }

        public LaunchBluetooth()
        {
            BleWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            BleWatcher.Received += BleReceived;
        }

        public static bool IsLaunchPaired()
        {
            var scope = new ManagementScope(@"\\" + Environment.MachineName + @"\root\CIMV2");
            var sq = new SelectQuery("SELECT Name FROM Win32_PnPEntity WHERE Name='Launch'");
            var searcher = new ManagementObjectSearcher(scope, sq);
            var moc = searcher.Get();

            foreach (ManagementObject mo in moc)
            {
                object propName = mo.Properties["Name"].Value;
                Debug.WriteLine($"{propName} is paired!");
                return true;
            }

            return false;
        }

        private async void BleReceived(BluetoothLEAdvertisementWatcher w, BluetoothLEAdvertisementReceivedEventArgs btAdv)
        {
            if (w == null) return;
            if (btAdv == null) return;

            if (connectionInProgress) return;

            Debug.WriteLine($"BLE advertisement received, aquiring device ...");

            if (btAdv.Advertisement.LocalName != "Launch")
            {
                Debug.WriteLine("Not a Launch");
                return;
            }

            connectionInProgress = true;

            try
            {
                BluetoothLEDevice device = await BluetoothLEDevice.FromBluetoothAddressAsync(btAdv.BluetoothAddress);

                Debug.WriteLine($"BLE Device: {device.Name} ({device.DeviceId})");
                GattDeviceService service = (await device.GetGattServicesForUuidAsync(Launch.Uids.MainService))
                    .Services.FirstOrDefault();
                if (service == null) return;
                Debug.WriteLine($"{device.Name} Main Services found!");
                Debug.WriteLine("Service UUID found!");

                GattCharacteristic writeCharacteristics =
                    (await service.GetCharacteristicsForUuidAsync(Launch.Uids.WriteCharacteristics)).Characteristics
                    .FirstOrDefault();
                GattCharacteristic notifyCharacteristics =
                    (await service.GetCharacteristicsForUuidAsync(Launch.Uids.StatusNotificationCharacteristics))
                    .Characteristics.FirstOrDefault();
                GattCharacteristic commandCharacteristics =
                    (await service.GetCharacteristicsForUuidAsync(Launch.Uids.CommandCharacteristics))
                    .Characteristics.FirstOrDefault();

                if (writeCharacteristics == null || commandCharacteristics == null ||
                    notifyCharacteristics == null)
                {
                    Debug.WriteLine("Characteristics not found!");
                    device.Dispose();
                    return;
                }

                Debug.WriteLine("Characteristics found!");

                Launch launch = new Launch(device, writeCharacteristics, notifyCharacteristics,
                    commandCharacteristics);

                bool init = await launch.Initialize();

                Debug.WriteLine("Launch Initialized: " + init.ToString().ToUpper() + "!");

                OnDeviceFound(launch);
                w.Stop();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                connectionInProgress = false;
            }
        }
    }
}
