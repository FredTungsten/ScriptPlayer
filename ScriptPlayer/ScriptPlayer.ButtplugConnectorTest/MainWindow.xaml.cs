using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Buttplug.Core;
using Buttplug.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ScriptPlayer.ButtplugConnectorTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ButtplugConnector _connector;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            _connector = new ButtplugConnector();
            await _connector.Connect();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _connector.StartScanning();
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            await _connector.SetPosition(99, 30);
            //await Task.Delay(TimeSpan.FromMilliseconds(300));
            //await _connector.SetPosition(0, 30);
        }
    }

    public class ButtplugConnector
    {
        private static Dictionary<string,Type> messageTypes;

        static ButtplugConnector()
        {
            messageTypes = typeof(ButtplugMessage).Assembly.GetTypes()
                .Where(t => typeof(ButtplugMessage).IsAssignableFrom(t))
                .ToDictionary(t => t.Name, t => t);
        }

        private TimeSpan _timeout = TimeSpan.FromMinutes(5);

        private ClientWebSocket _client;

        public async Task Connect()
        {
            CancellationTokenSource source = new CancellationTokenSource(_timeout);
            _client = new ClientWebSocket();
            await _client.ConnectAsync(new Uri("ws://localhost:12345/buttplug"), source.Token);

            var response = await Send(new RequestServerInfo("Test client"));

            source.Dispose();
        }

        private async Task<IEnumerable<ButtplugMessage>> Send(params ButtplugMessage[] messages)
        {
            CancellationTokenSource source = new CancellationTokenSource(_timeout);
            await _client.SendAsync(Jsonify(messages), WebSocketMessageType.Text, true, source.Token);
            source.Dispose();

            source = new CancellationTokenSource(TimeSpan.FromSeconds(50));
            byte[] byteBuffer = new byte[2048];
            var buffer = new ArraySegment<byte>(byteBuffer);
            var result = await _client.ReceiveAsync(buffer, source.Token);
            source.Dispose();

            string response = Encoding.UTF8.GetString(byteBuffer, 0, result.Count);

            var messageArray = Dejsonify(response);
            return messageArray;
        }

        private IEnumerable<ButtplugMessage> Dejsonify(string response)
        {
            var array = JArray.Parse(response);

            List<ButtplugMessage> result = new List<ButtplugMessage>();

            foreach (JObject messageWrapper in array)
            {
                var message = messageWrapper.Properties().First();

                Type type = messageTypes[message.Name];

                ButtplugMessage msg = (ButtplugMessage)message.Value.ToObject(type);
                
                result.Add(msg);
            }

            return result;
        }

        public async void StartScanning()
        {
            await Send(new StartScanning());
        }

        private ArraySegment<byte> Jsonify(params ButtplugMessage[] messages)
        {
            JArray array = new JArray();

            foreach (ButtplugMessage message in messages)
            {
                JObject wrapper = new JObject();
                wrapper[message.GetType().Name] = JObject.FromObject(message);
                array.Add(wrapper);
            }

            string json = JsonConvert.SerializeObject(array);
            return new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
        }

        public async Task SetPosition(byte position, byte speed)
        {
            await Send(new FleshlightLaunchFW12Cmd(0, speed, position));
        }
    }
}
