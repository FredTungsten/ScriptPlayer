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
        private readonly Dictionary<ulong, DateTime> _lastChecked = new Dictionary<ulong, DateTime>();
        private readonly List<ulong> _nonLaunchDevices = new List<ulong>();

        private readonly object _discoverylocker = new object();
        private bool _discover;
        public BluetoothLEAdvertisementWatcher BleWatcher { get; set; }
        
        public override void ScanForDevices()
        {
            Start();
        }

        public LaunchBluetooth()
        {
            BleWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            BleWatcher.Received += BleReceived;
        }

        public void Start()
        {
            lock (_discoverylocker)
            {
                if (_discover)
                    return;

                _lastChecked.Clear();
                _nonLaunchDevices.Clear();

                Debug.WriteLine("Start watching for BLE devices ...");
                _discover = true;
                BleWatcher.Start();
            }
        }

        public void Stop()
        {
            lock (_discoverylocker)
            {
                if (!_discover)
                    return;

                _discover = false;
                BleWatcher.Stop();
            }
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

            TimeSpan minTimeBetweenChecks = TimeSpan.FromSeconds(10);

            lock (_discoverylocker)
            {
                if (!_discover) return;
                //Stop();

                if(_nonLaunchDevices.Contains(btAdv.BluetoothAddress))
                    return;

                if(!_lastChecked.ContainsKey(btAdv.BluetoothAddress))
                    _lastChecked.Add(btAdv.BluetoothAddress, DateTime.Now);
                else if (DateTime.Now - _lastChecked[btAdv.BluetoothAddress] < minTimeBetweenChecks)
                    return;

                _lastChecked[btAdv.BluetoothAddress] = DateTime.Now;
            }

            Debug.WriteLine($"BLE advertisement received, aquiring device ...");

            var deviceAwaiting = BluetoothLEDevice.FromBluetoothAddressAsync(btAdv.BluetoothAddress);

            if (deviceAwaiting == null) return;

            BluetoothLEDevice device = await deviceAwaiting;

            if (device == null) return;

            Debug.WriteLine($"BLE Device: {device.Name} ({device.DeviceId})");

            if (device.Name != "Launch")
            {
                Debug.WriteLine("Not a Launch");
                _nonLaunchDevices.Add(device.BluetoothAddress);
                device.Dispose();
                return;
            }

            bool foundAndConnected = false;

            try
            {
                Thread.Sleep(1000);
                // SERVICES!!
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
                    notifyCharacteristics == null) return;

                Debug.WriteLine("Characteristics found!");

                Launch launch = new Launch(device, writeCharacteristics, notifyCharacteristics,
                    commandCharacteristics);

                bool init = await launch.Initialize();

                Debug.WriteLine("Launch Initialized: " + init.ToString().ToUpper() + "!");

                foundAndConnected = true;
                OnDeviceFound(launch);
                Stop();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message);
                device.Dispose();
            }
            finally
            {
                if (!foundAndConnected)
                {
                    Debug.WriteLine("Connect failed, try again ...");
                    Start();
                }
            }
        }
    }
}
