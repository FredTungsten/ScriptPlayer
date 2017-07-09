using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace ScriptPlayer.Shared
{
    public class LaunchBluetooth
    {
        private readonly object _discoverylocker = new object();
        private bool _discover;
        public BluetoothLEAdvertisementWatcher BleWatcher { get; set; }

        public delegate void DeviceFoundEventHandler(object sender, Launch device);

        public event DeviceFoundEventHandler DeviceFound;

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
            
                Debug.WriteLine("Start watching ...");
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

        private async void BleReceived(BluetoothLEAdvertisementWatcher w, BluetoothLEAdvertisementReceivedEventArgs btAdv)
        {
            lock (_discoverylocker)
            {
                if (!_discover) return;
                Stop();
            }

            Debug.WriteLine($"BLE RECEIVED, Services: {String.Join(", ", btAdv.Advertisement.ServiceUuids)}, aquiring device ...");

            var deviceAwaiting = BluetoothLEDevice.FromBluetoothAddressAsync(btAdv.BluetoothAddress);

            if (deviceAwaiting == null) return;

            BluetoothLEDevice device = await deviceAwaiting;

            Debug.WriteLine($"BLEWATCHER Found: {device.Name}, {device.DeviceId}");

            bool foundAndConnected = false;

            try
            {
                Thread.Sleep(3000);
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

            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message);
                if (device != null)
                {
                    device.Dispose();
                }
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

        protected virtual void OnDeviceFound(Launch device)
        {
            DeviceFound?.Invoke(this, device);
        }
    }
}
