using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ScriptPlayer.ButtplugConnector
{
    public class ButtplugWebSocketConnector
    {
        private static readonly Dictionary<string, Type> MessageTypes;

        static ButtplugWebSocketConnector()
        {
            MessageTypes = typeof(ButtplugMessage).Assembly.GetTypes()
                .Where(t => typeof(ButtplugMessage).IsAssignableFrom(t))
                .ToDictionary(t => t.Name, t => t);
        }

        private readonly TimeSpan _timeout = TimeSpan.FromMinutes(5);

        private ClientWebSocket _client;

        private long _messageId = 1;
        private Thread _readThread;
        private bool _running;

        private readonly object _callbackLocker = new object();
        private readonly Dictionary<uint, ButtplugPromise> _registeredPromises = new Dictionary<uint, ButtplugPromise>();

        private void AddPromise(uint messageId, ButtplugPromise promise)
        {
            if (promise == null)
                return;

            lock (_callbackLocker)
            {
                _registeredPromises.Add(messageId, promise);
            }
        }

        private ButtplugPromise GetPromise(uint messageId)
        {
            lock (_callbackLocker)
            {
                if (_registeredPromises.ContainsKey(messageId))
                {
                    var result = _registeredPromises[messageId];
                    _registeredPromises.Remove(messageId);
                    return result;
                }
                return null;
            }
        }

        private uint GetNextMessageId()
        {
            long msg = Interlocked.Increment(ref _messageId);
            return (uint)(msg % uint.MaxValue);
        }

        public async Task<bool> Connect(string uri = "ws://localhost:12345/buttplug")
        {
            try
            {
                CancellationTokenSource source = new CancellationTokenSource(_timeout);
                _client = new ClientWebSocket();
                await _client.ConnectAsync(new Uri(uri), source.Token);
                source.Dispose();
            }
            catch (WebSocketException)
            {
                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            StartReadQueue();

            Send<ServerInfo>(new RequestServerInfo("ScriptPlayer")).Then(ServerInfoResponse, GenericFailure);
            Send<DeviceList>(new RequestDeviceList()).Then(DeviceListReceived, GenericFailure);

            return true;
        }

        public async Task Disconnect()
        {
            _running = false;

            _readThreadCancellationSource?.Cancel();

            if (!_readThread.Join(500))
                _readThread.Abort();

            _client.Dispose();
        }

        private void DeviceListReceived(DeviceList list)
        {
            if (list.Devices.Length == 0)
            {
                StartScanning();
            }
            else
            {
                ClearDevices();
                foreach (DeviceMessageInfo device in list.Devices)
                {
                    AddDevice(device.DeviceName ?? "Unknown Device", device.DeviceIndex);
                }
            }
        }

        private void GenericFailure(ButtplugMessage response)
        {
            if (response is Error)
            {
                Error error = (Error)response;
                Debug.WriteLine($"Error in Connect: {error.Id} - {error.ErrorMessage}");
            }
            else
            {
                Debug.WriteLine($"Unexpected Message Type: {response.GetType()}");
            }
        }

        private void ServerInfoResponse(ServerInfo info)
        {
            Debug.WriteLine($"Connected to {info.ServerName} Version {info.MajorVersion}.{info.MinorVersion}.{info.BuildVersion}, MessageVersion {info.MessageVersion}");
        }

        private void StartReadQueue()
        {
            _running = true;
            _readThread = new Thread(ReadThread);
            _readThread.Start();
        }

        private async void ReadThread()
        {
            while (_running)
            {
                try
                {
                    _readThreadCancellationSource = new CancellationTokenSource(TimeSpan.FromDays(1));
                    byte[] byteBuffer = new byte[2048];
                    var buffer = new ArraySegment<byte>(byteBuffer);
                    var result = await _client.ReceiveAsync(buffer, _readThreadCancellationSource.Token);
                    _readThreadCancellationSource.Dispose();

                    string response = Encoding.UTF8.GetString(byteBuffer, 0, result.Count);

                    try
                    {
                        
                        var messageArray = Dejsonify(response);

                        foreach (ButtplugMessage message in messageArray)
                        {
                            HandleMessage(message);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private void HandleMessage(ButtplugMessage message)
        {
            var callback = GetPromise(message.Id);
            if (callback != null)
            {
                callback.SetResult(message);
            }
            else
            {
                if (message is DeviceAdded)
                {
                    DeviceAdded added = (DeviceAdded)message;
                    Debug.WriteLine($"Device Added: {added.DeviceName} [{added.DeviceIndex}]");
                    AddDevice(added.DeviceName, added.DeviceIndex);
                }
                else if (message is DeviceRemoved)
                {
                    DeviceRemoved removed = (DeviceRemoved)message;
                    Debug.WriteLine($"Device Removed: [{removed.DeviceIndex}]");
                    RemoveDevice(removed.DeviceIndex);
                }
                else
                {
                    Debug.WriteLine("Unknown messageId");
                }
            }
        }


        private readonly Dictionary<uint, string> _devices = new Dictionary<uint, string>();
        private CancellationTokenSource _readThreadCancellationSource;

        private void ClearDevices()
        {
            _devices.Clear();
        }
        private void AddDevice(string name, uint index)
        {
            _devices.Add(index, name);
        }

        private void RemoveDevice(uint index)
        {
            _devices.Remove(index);
        }

        private ButtplugPromise<T> Send<T>(ButtplugMessage message) where T : ButtplugMessage
        {
            ButtplugPromise<T> promise = new ButtplugPromise<T>();
            Send(message, promise);
            return promise;
        }

        private async void Send(ButtplugMessage message, ButtplugPromise promise)
        {
            message.Id = GetNextMessageId();

            AddPromise(message.Id, promise);

            try
            {
                CancellationTokenSource source = new CancellationTokenSource(_timeout);
                await _client.SendAsync(Jsonify(message), WebSocketMessageType.Text, true, source.Token);
                source.Dispose();
            }
            catch (OperationCanceledException)
            {
                promise.Cancel();
                GetPromise(message.Id);
            }
        }

        private IEnumerable<ButtplugMessage> Dejsonify(string response)
        {
            var array = JArray.Parse(response);

            List<ButtplugMessage> result = new List<ButtplugMessage>();

            foreach (var jToken in array)
            {
                var messageWrapper = (JObject) jToken;
                var message = messageWrapper.Properties().First();

                Type type = MessageTypes[message.Name];

                ButtplugMessage msg = (ButtplugMessage)message.Value.ToObject(type);

                result.Add(msg);
            }

            return result;
        }

        public ButtplugPromise<Ok> StartScanning()
        {
            return Send<Ok>(new StartScanning());
        }

        private ArraySegment<byte> Jsonify(params ButtplugMessage[] messages)
        {
            JArray array = new JArray();

            foreach (ButtplugMessage message in messages)
            {
                JObject wrapper = new JObject
                {
                    // Indexed object initializers?
                    // Cool, I completely missed, that those existed 
                    [message.GetType().Name] = JObject.FromObject(message)
                };
                array.Add(wrapper);
            }

            string json = JsonConvert.SerializeObject(array);
            return new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
        }

        public void SetPosition(byte position, byte speed)
        {
            if (_devices.Count == 0)
                return;

            Send<Ok>(new FleshlightLaunchFW12Cmd(_devices.First().Key, speed, position)).Then(SetPositionSuccess, GenericFailure);
        }

        private void SetPositionSuccess(Ok obj)
        {

        }
    }
}