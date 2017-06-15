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
    public abstract class MessageCallback
    {
        public abstract void Callback(ButtplugMessage result);
    }

    public class ButtplugMessageCallback<T> : MessageCallback where T : ButtplugMessage
    {
        private readonly Action<T> _onSuccess;
        private readonly Action<ButtplugMessage> _onFailure;

        public ButtplugMessageCallback(Action<T> onSuccess, Action<ButtplugMessage> onFailure)
        {
            _onSuccess = onSuccess;
            _onFailure = onFailure;
        }

        public override void Callback(ButtplugMessage result)
        {
            if (result is T)
                _onSuccess((T)result);
            else
                _onFailure(result);
        }
    }

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
        private readonly Dictionary<uint, MessageCallback> _registeredCallbacks = new Dictionary<uint, MessageCallback>();

        private void AddCallback(uint messageId, MessageCallback callback)
        {
            if (callback == null)
                return;

            lock (_callbackLocker)
            {
                _registeredCallbacks.Add(messageId, callback);
            }
        }

        private MessageCallback GetCallback(uint messageId)
        {
            lock (_callbackLocker)
            {
                if (_registeredCallbacks.ContainsKey(messageId))
                    return _registeredCallbacks[messageId];
                return null;
            }
        }

        private uint GetNextMessageId()
        {
            long msg = Interlocked.Increment(ref _messageId);
            return (uint)(msg % uint.MaxValue);
        }

        public async Task Connect(string uri = "ws://localhost:12345/buttplug")
        {
            CancellationTokenSource source = new CancellationTokenSource(_timeout);
            _client = new ClientWebSocket();
            await _client.ConnectAsync(new Uri(uri), source.Token);
            source.Dispose();

            StartReadQueue();

            await Send(new RequestServerInfo("ScriptPlayer"), new ButtplugMessageCallback<ServerInfo>(ServerInfoResponse, GenericFailure));
            await Send(new RequestDeviceList(), new ButtplugMessageCallback<DeviceList>(DeviceListReceived, GenericFailure));
        }

        public async Task Disconnect()
        {
            _running = false;

            if(_readThreadCancellationSource != null)
                _readThreadCancellationSource.Cancel();

            if (!_readThread.Join(500))
                _readThread.Abort();

            await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close",
                new CancellationTokenSource(2000).Token);
        }

        private async void DeviceListReceived(DeviceList list)
        {
            if (list.Devices.Length == 0)
            {
                await StartScanning();
            }
            else
            {
                ClearDevices();
                foreach (var device in list.Devices)
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
            var callback = GetCallback(message.Id);
            if (callback != null)
            {
                callback.Callback(message);
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

        private async Task Send(ButtplugMessage message, MessageCallback callback)
        {
            message.Id = GetNextMessageId();

            AddCallback(message.Id, callback);

            CancellationTokenSource source = new CancellationTokenSource(_timeout);
            await _client.SendAsync(Jsonify(message), WebSocketMessageType.Text, true, source.Token);
            source.Dispose();
        }

        private IEnumerable<ButtplugMessage> Dejsonify(string response)
        {
            var array = JArray.Parse(response);

            List<ButtplugMessage> result = new List<ButtplugMessage>();

            foreach (JObject messageWrapper in array)
            {
                var message = messageWrapper.Properties().First();

                Type type = MessageTypes[message.Name];

                ButtplugMessage msg = (ButtplugMessage)message.Value.ToObject(type);

                result.Add(msg);
            }

            return result;
        }

        public async Task StartScanning()
        {
            await Send(new StartScanning(), new ButtplugMessageCallback<Ok>(ScanStarted, GenericFailure));
        }

        private void ScanStarted(Ok obj)
        {
            Debug.WriteLine("Ok");
        }

        private T Expect<T>(ButtplugMessage message) where T : ButtplugMessage
        {
            if (message is T)
            {
                return (T)message;
            }

            if (message is Error)
            {
                Error error = (Error)message;
                Debug.WriteLine($"Error in Connect: {error.Id} - {error.ErrorMessage}");
                return null;
            }

            Debug.WriteLine($"Unexpected Message Type: {message.GetType()}");
            return null;
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

        public async Task SetPosition(byte position, byte speed)
        {
            if (_devices.Count == 0)
                return;

            await Send(new FleshlightLaunchFW12Cmd(_devices.First().Key, speed, position), new ButtplugMessageCallback<Ok>(SetPositionSuccess, GenericFailure));
        }

        private void SetPositionSuccess(Ok obj)
        {

        }
    }
}