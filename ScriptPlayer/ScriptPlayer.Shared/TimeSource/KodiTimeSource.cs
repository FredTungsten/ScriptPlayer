using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Net.WebSockets;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;

namespace ScriptPlayer.Shared
{
    public class KodiTimeSource : TimeSource, IDisposable
    {
        private KodiConnectionSettings _connectionSettings;

        public static readonly DependencyProperty IsConnectedProperty = DependencyProperty.Register(
            "IsConnected", typeof(bool), typeof(KodiTimeSource), new PropertyMetadata(default(bool)));

        public bool IsConnected
        {
            get => (bool)GetValue(IsConnectedProperty);
            set => SetValue(IsConnectedProperty, value);
        }

        public event EventHandler<string> FileOpened;

        private readonly Thread _clientLoop;
        private readonly ManualTimeSource _timeSource;

        private ClientWebSocket _websocket;
        private readonly CancellationTokenSource _cts;

        private bool _running = true;

        private static Task SendString(ClientWebSocket ws, string data, CancellationToken cancellation)
        {
            var encoded = Encoding.UTF8.GetBytes(data);
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            return ws.SendAsync(buffer, WebSocketMessageType.Text, true, cancellation);
        }

        private static void SendStringSync(ClientWebSocket ws, string data, CancellationToken ct)
        {
            var task = SendString(ws, data, ct);
            task.Wait();
        }

        private static async Task<string> ReadString(ClientWebSocket ws)
        {
            ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[8192]);

            WebSocketReceiveResult result = null;

            using (var ms = new MemoryStream())
            {
                do
                {
                    result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(ms, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }

        private static string ReadStringSync(ClientWebSocket ws, CancellationToken ct)
        {
            var task = ReadString(ws);
            try
            {
                task.Wait(ct);
            }
            catch(Exception e)
            {
                return null;
            }

            return task.Result;
        }

        public KodiTimeSource(ISampleClock clock, KodiConnectionSettings settings)
        {
            _connectionSettings = settings;

            // figure out what clock and offset do
            _timeSource = new ManualTimeSource(clock, TimeSpan.FromMilliseconds(200));
            _timeSource.DurationChanged += TimeSourceOnDurationChanged;
            _timeSource.ProgressChanged += TimeSourceOnProgressChanged;
            _timeSource.IsPlayingChanged += TimeSourceOnIsPlayingChanged;
            _timeSource.PlaybackRateChanged += TimeSourceOnPlaybackRateChanged;

            _cts = new CancellationTokenSource();

            _clientLoop = new Thread(ClientLoop);
            _clientLoop.Start();
        }

        private void InterpretKodiMsg(string json)
        {
            if (!_timeSource.CheckAccess())
            {
                _timeSource.Dispatcher.Invoke(() => InterpretKodiMsg(json));
                return;
            }

            JObject json_obj;
            try
            {
                json_obj = JObject.Parse(json);
            }
            catch(Exception e)
            {
                return;
            }


            double hours, minutes, seconds, milliseconds;

            string method = json_obj["method"]?.ToString();
            switch(method)
            {
                case "Playlist.OnAdd":
                    break;
                case "Player.OnPlay": // use this on older kodi versions
                    // OnPlay will occur before the video is actually playing
                    Console.WriteLine("OnPlay");
                    break;
                case "Player.OnAVStart": // only available since Kodi 18 :( https://kodi.wiki/view/JSON-RPC_API/v9
                    // OnAVStart occurs when the first frame is drawn
                    {
                        Console.WriteLine("AVStart");
                        Pause(); // pause because of all the synchronous http post requests and could get badly out of sync

                        string filename = json_obj["params"]["data"]["item"]["title"]?.ToString();
                        if (filename != null)
                        {
                            OnFileOpened(filename);
                        }
                        else
                        {
                            // get the filename via http json api
                            string json_data;
                            if (Request("{\"jsonrpc\": \"2.0\", \"method\": \"Player.GetItem\", \"params\": { \"properties\": [\"file\"], \"playerid\": 1 }, \"id\": \"VideoGetItem\"}", out json_data))
                            {
                                var json_data_obj = JObject.Parse(json_data);
                                filename = json_data_obj["result"]["item"]["file"]?.ToString();
                            }

                            OnFileOpened(filename);
                        }

                        string json_duration;
                        if (Request("{\"jsonrpc\": \"2.0\", \"method\": \"Player.GetProperties\", \"params\": { \"properties\": [\"totaltime\"], \"playerid\": 1 }, \"id\": \"VideoGetProp\"}", out json_duration))
                        {
                            var json_duration_obj = JObject.Parse(json_duration);
                            var duration = json_duration_obj["result"]["totaltime"];

                            if (!double.TryParse(duration["hours"]?.ToString(), out hours)) return;
                            if (!double.TryParse(duration["minutes"]?.ToString(), out minutes)) return;
                            if (!double.TryParse(duration["seconds"]?.ToString(), out seconds)) return;
                            if (!double.TryParse(duration["milliseconds"]?.ToString(), out milliseconds)) return;
                            TimeSpan duration_span = TimeSpan.FromHours(hours);
                            duration_span += TimeSpan.FromMinutes(minutes);
                            duration_span += TimeSpan.FromSeconds(seconds);
                            duration_span += TimeSpan.FromMilliseconds(milliseconds);
                            _timeSource.SetDuration(duration_span);
                        }

                        // this is not good if the video had a resume point but was started from the begining scriptplayer will skip to the resume point
                        /*
                        string json_resume;
                        if (Request("{\"jsonrpc\": \"2.0\", \"method\": \"Player.GetItem\", \"params\": { \"properties\": [\"resume\"], \"playerid\": 1 }, \"id\": \"VideoGetResume\"}", out json_resume))
                        {
                            var json_resume_obj = JObject.Parse(json_resume);
                            double resume_seconds = double.Parse((string)json_resume_obj["result"]["item"]["resume"]["position"]);
                            _timeSource.SetPosition(TimeSpan.FromSeconds(resume_seconds));
                        }
                        */

                        Play();
                        _timeSource.Play();
                        break;
                    }
                case "Player.OnPause":
                    {
                        _timeSource.Pause();
                        break;
                    }
                case "Player.OnResume":
                    {
                        _timeSource.Play();
                        break;
                    }
                case "Player.OnStop":
                    {
                        Console.WriteLine("stop playback");
                        _timeSource.Pause();
                        break;
                    }
                case "Player.OnSeek":
                    {
                        JObject time = (JObject)json_obj["params"]["data"]["player"]["time"];
                        if(time != null)
                        {
                            if(!double.TryParse(time["hours"]?.ToString(), out hours)) return;
                            if(!double.TryParse(time["minutes"]?.ToString(), out minutes)) return;
                            if(!double.TryParse(time["seconds"]?.ToString(), out seconds)) return;
                            if(!double.TryParse(time["milliseconds"]?.ToString(), out milliseconds)) return;

                            TimeSpan pos = TimeSpan.FromHours(hours);
                            pos += TimeSpan.FromMinutes(minutes);
                            pos += TimeSpan.FromSeconds(seconds);
                            pos += TimeSpan.FromMilliseconds(milliseconds);
                            Console.WriteLine("new pos:" + pos.ToString());
                            _timeSource.SetPosition(pos);
                        }
                        break;
                    }
                case null:
                    {
                        Console.WriteLine("no method");
                        return;
                    }

                default:
                    {
                        Console.WriteLine("Unhandled method: " + method);
                        break;
                    }
                    
            }
        }

        private bool Connect() 
        {
            var uri = new Uri("ws://" + _connectionSettings.Ip + ":" + _connectionSettings.TcpPort + "/jsonrpc");
            while (true)
            {
                Console.WriteLine("connecting...");
                _websocket = new ClientWebSocket();
                var connected = _websocket.ConnectAsync(uri, _cts.Token);
                try
                {
                    connected.Wait();
                    return true;
                }
                catch (AggregateException e)
                {
                    if(e.InnerException.GetType() == typeof(WebSocketException))
                    {
                        // probably unable to connect to kodi                 
                    }
                    else
                    {
                        // something else happened
                        throw e;
                    }
                }

                Thread.Sleep(250); // cooldown  
            }

        }

        private void ClientLoop()
        {
            Connect();

            {
                while(true)
                {
                    string response;
                    if (Request("{\"jsonrpc\": \"2.0\", \"method\": \"Player.GetActivePlayers\", \"id\": 69}", out response))
                    {
                        SetConnected(true);
                        break;
                    }
                    else
                    {
                        SetConnected(false);
                    }
                    Thread.Sleep(500); // cooldown
                }

            }

            // send any json command afterwards kodi starts sending messages about it's status back
            SendStringSync(_websocket, "{\"jsonrpc\": \"2.0\", \"method\": \"Player.GetActivePlayers\", \"id\": 69}", _cts.Token);
            while (_running)
            {
                var result = ReadStringSync(_websocket, _cts.Token);
                Console.WriteLine("msg: " + result);
                InterpretKodiMsg(result);
            }
        }

        private void SetConnected(bool isConnected)
        {
            if (CheckAccess())
                IsConnected = isConnected;
            else
                Dispatcher.Invoke(() => { SetConnected(isConnected); });
        }

        public bool Request(string json, out string response)
        {
            try
            {
                var client = new WebClient();

                string userName = _connectionSettings.User;
                string password = _connectionSettings.Password;
                string baseUrl = $"http://{_connectionSettings.Ip}:{_connectionSettings.HttpPort}/jsonrpc";

                //string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName + ":" + password));
                string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + password));
                client.Headers[HttpRequestHeader.Authorization] = $"Basic {credentials}";
                client.Headers[HttpRequestHeader.ContentType] = "application/json";

                var result = client.UploadString(baseUrl, "POST", json);
                Console.WriteLine("request: " + result);
                response = result;
                return true;
            }
            catch(WebException e)
            {
                try
                {
                    if(((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.Unauthorized)
                    {
                        // unauthorized
                        Console.WriteLine("failed authorization");
                    }
                }
                catch(Exception) { }

            }
            catch(Exception)
            {
            }
            response = null;
            return false;
        }

        protected virtual void OnFileOpened(string e)
        {
            FileOpened?.Invoke(this, e);
        }

        private void TimeSourceOnDurationChanged(object sender, TimeSpan duration)
        {
            Duration = duration;
        }

        private void TimeSourceOnIsPlayingChanged(object sender, bool isPlaying)
        {
            IsPlaying = isPlaying;
        }

        private void TimeSourceOnProgressChanged(object sender, TimeSpan progress)
        {
            Progress = progress;
        }

        private void TimeSourceOnPlaybackRateChanged(object sender, double d)
        {
            OnPlaybackRateChanged(d);
        }

        public override double PlaybackRate
        {
            get => _timeSource.PlaybackRate;
            set => _timeSource.PlaybackRate = value;
        }

        public override bool CanPlayPause => true;

        public override bool CanSeek => true;

        public override bool CanOpenMedia => true;

        public void Dispose()
        {
            _running = false;
            _cts.Cancel();
            _clientLoop?.Interrupt();
            _clientLoop?.Abort();
        }

        public override void Pause()
        {
            //throw new NotImplementedException();
            //SendStringSync(_websocket, "{\"jsonrpc\": \"2.0\", \"method\": \"Player.PlayPause\", \"id\": 1}", _cts.Token);
            string response;
            Request("{\"jsonrpc\": \"2.0\", \"method\": \"Player.PlayPause\", \"params\": { \"playerid\": 1 }, \"id\": 1}", out response);
        }

        public override void Play()
        {
            string response;
            Request("{\"jsonrpc\": \"2.0\", \"method\": \"Player.PlayPause\", \"params\": { \"playerid\": 1 }, \"id\": 1}", out response);
            //throw new NotImplementedException();
            //SendStringSync(_websocket, "{\"jsonrpc\": \"2.0\", \"method\": \"Player.PlayPause\", \"id\": 1}", _cts.Token);
        }

        public override void SetPosition(TimeSpan position)
        {
            dynamic pos = new JObject();
            
            pos.hours = position.Hours;
            pos.minutes = position.Minutes;
            pos.seconds = position.Seconds;
            pos.milliseconds = position.Milliseconds;

            string response;
            Request("{\"jsonrpc\": \"2.0\", \"method\": \"Player.Seek\", \"params\": {\"value\":" + pos.ToString(Formatting.None, null) +  ", \"playerid\": 1 }, \"id\": 1}", out response);
            //throw new NotImplementedException();
        }
    }
}
