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

/*
 * Done: Handle when a video is resumed: tested although not always the best accuracy
 * Done: Handle playback speed? there doesn't seem to be a point since it's not granular enough goes from 2 -> 4 -> 8 -> 16 -> 36
 * TODO: test playing videos from different sources in kodi (FTP, UPNP, ...)
 * Done: evaluate if InterpretKodiMsgNew has any benefit since InterpretKodiMsgLegacy seems pretty robust and should work with newer version aswell with slight modification (OnResume/OnPlay). yes.
 */

/*
 * Tested with:
 * Kodi 18.1 on Linux over LAN
 * Kodi 17.6 on Windows on localhost
 * and Kodi 15 on Windows on localhost
 * 
 * Kodi playback sources tested: local disk, smb share
 */
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

                                            // v10 = Kodi 18.1 mine returns 10                           
        private int _api_major_version = 8; // v9 = Kodi 18
                                            // v8 = Kodi 17

        private bool _OnAdd_happened = false; // for legacy kodi api


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
            ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[65536]);

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

        private string GetCurrentPlayingFile()
        {
            string json_data;
            // this actually returns a path with the filename
            if (Request("{\"jsonrpc\": \"2.0\", \"method\": \"Player.GetItem\", \"params\": { \"properties\": [\"file\"], \"playerid\": 1 }, \"id\": \"VideoGetItem\"}", out json_data))
            {
                var json_data_obj = JObject.Parse(json_data);
                string filepath = json_data_obj["result"]["item"]["file"]?.ToString();             

                // make smb path windows compatible
                if(filepath.StartsWith("smb:"))
                {
                    filepath = filepath.Replace("smb:", "");
                    filepath = filepath.Replace('/', '\\');
                }
                Console.WriteLine("currently playing: " + filepath);

                return filepath;
            }
            else
            {
                //error
                return "";
            }
        }

        private TimeSpan GetCurrentDuration()
        {
            string json_duration;
            if (Request("{\"jsonrpc\": \"2.0\", \"method\": \"Player.GetProperties\", \"params\": { \"properties\": [\"totaltime\"], \"playerid\": 1 }, \"id\": \"VideoGetProp\"}", out json_duration))
            {
                double hours, minutes, seconds, milliseconds;
                var json_duration_obj = JObject.Parse(json_duration);
                var duration = json_duration_obj["result"]["totaltime"];

                if (!double.TryParse(duration["hours"]?.ToString(), out hours)) return TimeSpan.Zero;
                if (!double.TryParse(duration["minutes"]?.ToString(), out minutes)) return TimeSpan.Zero;
                if (!double.TryParse(duration["seconds"]?.ToString(), out seconds)) return TimeSpan.Zero;
                if (!double.TryParse(duration["milliseconds"]?.ToString(), out milliseconds)) return TimeSpan.Zero;
                TimeSpan duration_span = TimeSpan.FromHours(hours);
                duration_span += TimeSpan.FromMinutes(minutes);
                duration_span += TimeSpan.FromSeconds(seconds);
                duration_span += TimeSpan.FromMilliseconds(milliseconds);
                return duration_span;
            }
            else
            {
                // request failed
                return TimeSpan.Zero;
            }
        }

        private TimeSpan GetCurrentTime()
        {
            string json_time;
            if (Request("{\"jsonrpc\": \"2.0\", \"method\": \"Player.GetProperties\", \"params\": { \"properties\": [\"time\"], \"playerid\": 1 }, \"id\": \"VideoGetProp\"}", out json_time))
            {
                double hours, minutes, seconds, milliseconds;
                var json_duration_obj = JObject.Parse(json_time);
                var time = json_duration_obj["result"]["time"];

                if (!double.TryParse(time["hours"]?.ToString(), out hours)) return TimeSpan.Zero;
                if (!double.TryParse(time["minutes"]?.ToString(), out minutes)) return TimeSpan.Zero;
                if (!double.TryParse(time["seconds"]?.ToString(), out seconds)) return TimeSpan.Zero;
                if (!double.TryParse(time["milliseconds"]?.ToString(), out milliseconds)) return TimeSpan.Zero;
                TimeSpan time_span = TimeSpan.FromHours(hours);
                time_span += TimeSpan.FromMinutes(minutes);
                time_span += TimeSpan.FromSeconds(seconds);
                time_span += TimeSpan.FromMilliseconds(milliseconds);
                Console.WriteLine(time_span);
                return time_span > TimeSpan.Zero ? time_span : TimeSpan.Zero;
            }
            else
            {
                // request failed
                return TimeSpan.Zero;
            }
        }

        private void InterpretKodiMsgNew(string json)
        {
            if (!_timeSource.CheckAccess())
            {
                _timeSource.Dispatcher.Invoke(() => InterpretKodiMsgNew(json));
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

            string method = json_obj["method"]?.ToString();
            switch(method)
            {                   
                case "Player.OnAVStart": // only available since api v9 https://kodi.wiki/view/JSON-RPC_API/v9
                    // OnAVStart occurs when the first frame is drawn
                    {
                        Pause(); // pause because of all the synchronous http post requests and could get badly out of sync

                        // always get the filepath via http json api
                        string filepath = GetCurrentPlayingFile();
                        OnFileOpened(filepath);

                        TimeSpan current_duration = GetCurrentDuration();
                        _timeSource.SetDuration(current_duration);

                        // for the times when the video was resumed
                        TimeSpan current_time = GetCurrentTime(); // there doesn't seem to be a race condition in Kodi 18.1 like in Kodi 15
                                                                  // needs to be tested on a slower device
                        _timeSource.SetPosition(current_time);

                        Play();
                        _timeSource.Play();
                        break;
                    }
                case "Player.OnPause":
                    {
                        _timeSource.Pause();
                        break;
                    }
                case "Player.OnResume": // also api v9+ lower apis will instead send OnPlay :/
                    {
                        _timeSource.Play();
                        break;
                    }
                case "Player.OnStop":
                    {
                        Console.WriteLine("stop playback");
                        _timeSource.Pause();
                        _timeSource.SetPosition(TimeSpan.Zero);
                        break;
                    }
                case "Player.OnSeek":
                    {
                        JObject time = (JObject)json_obj["params"]["data"]["player"]["time"];
                        if(time != null)
                        {
                            double hours, minutes, seconds, milliseconds;
                            if (!double.TryParse(time["hours"]?.ToString(), out hours)) return;
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

        private void InterpretKodiMsgLegacy(string json)
        {
            if (!_timeSource.CheckAccess())
            {
                _timeSource.Dispatcher.Invoke(() => InterpretKodiMsgLegacy(json));
                return;
            }

            JObject json_obj;
            try
            {
                json_obj = JObject.Parse(json);
            }
            catch (Exception e)
            {
                return;
            }
            string method = json_obj["method"]?.ToString();

            switch (method)
            {
                case "Playlist.OnAdd":
                    {
                        _OnAdd_happened = true;
                        break;
                    }
                case "Player.OnPlay": // use this on older kodi versions
                    {
                        Console.WriteLine("OnPlay");
                        if(_OnAdd_happened)
                        {
                            _OnAdd_happened = false;
                            Pause(); // pause because of all the synchronous http post requests and could get badly out of sync

                            // always get the filepath via http json api
                            string filepath = GetCurrentPlayingFile();
                            OnFileOpened(filepath);

                            TimeSpan current_duration = GetCurrentDuration();
                            _timeSource.SetDuration(current_duration);

                            Play();
                            _timeSource.Play();

                            Thread.Sleep(100); // on kodi 15 this is racy ughh might need to be adjusted upwards for slower devices like a raspberry pi
                            // for the times when the video was resumed
                            TimeSpan current_time = GetCurrentTime();
                            _timeSource.SetPosition(current_time);
                        }
                        else
                        {
                            _timeSource.Play();
                        }
                        break;
                    }
                case "Player.OnPause":
                    {
                        _timeSource.Pause();
                        break;
                    }
                case "Player.OnSeek":
                    {
                        double hours, minutes, seconds, milliseconds;
                        JObject time = (JObject)json_obj["params"]["data"]["player"]["time"];
                        if (time != null)
                        {
                            if (!double.TryParse(time["hours"]?.ToString(), out hours)) return;
                            if (!double.TryParse(time["minutes"]?.ToString(), out minutes)) return;
                            if (!double.TryParse(time["seconds"]?.ToString(), out seconds)) return;
                            if (!double.TryParse(time["milliseconds"]?.ToString(), out milliseconds)) return;

                            TimeSpan pos = TimeSpan.FromHours(hours);
                            pos += TimeSpan.FromMinutes(minutes);
                            pos += TimeSpan.FromSeconds(seconds);
                            pos += TimeSpan.FromMilliseconds(milliseconds);
                            Console.WriteLine("new pos:" + pos.ToString());
                            _timeSource.SetPosition(pos);
                        }
                        break;
                    }
                case "Player.OnStop":
                    {
                        Console.WriteLine("stop playback");
                        _timeSource.Pause();
                        _timeSource.SetPosition(TimeSpan.Zero);
                        break;
                    }
                case null:
                    {
                        return;
                    }
                default:
                    {
                        Console.WriteLine("Unhandled method: " + method);
                        break;
                    }
            }

        }


        private void ClientLoop()
        {
            Connect();
            {
                while(true)
                {
                    // test if http post works
                    string response;
                    if (Request("{\"jsonrpc\": \"2.0\", \"method\": \"Player.GetActivePlayers\", \"id\": 69}", out response))
                    {
                        SetConnected(true);
                        break;
                    }
                    else
                    {
                        // something failed: auth, wrong ip ports etc.
                        SetConnected(false);
                    }
                    Thread.Sleep(500); // cooldown
                }

                // get api version
                string api_version;
                if(Request("{\"jsonrpc\": \"2.0\", \"method\": \"JSONRPC.Version\", \"id\": 1}", out api_version))
                {
                    var json_version_obj = JObject.Parse(api_version);
                    this._api_major_version = (int)json_version_obj["result"]["version"]["major"];
                    Console.WriteLine("found api version: " + _api_major_version);
                }
            }


            // actual loop
            void RunAPI_version(Action<string> handle_msg)
            {
                // send any json command over the websocket afterwards kodi starts sending messages about it's status back
                SendStringSync(_websocket, "{\"jsonrpc\": \"2.0\", \"method\": \"Player.GetActivePlayers\", \"id\": 69}", _cts.Token);
                while (_running)
                {
                    var result = ReadStringSync(_websocket, _cts.Token);
                    if (result == null)
                    {
                        // attempt to reconnect
                        SetConnected(false);
                        Connect();
                        SetConnected(true);
                    }
                    Console.WriteLine("msg: " + result);
                    handle_msg(result);
                }
            }

            //depending on the api version run a different version
            if(_api_major_version >= 9)
            {
                RunAPI_version(InterpretKodiMsgNew);
            }
            else
            {
                RunAPI_version(InterpretKodiMsgLegacy);
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
                    if (e.InnerException.GetType() == typeof(WebSocketException))
                    {
                        // probably unable to connect to kodi
                    }
                    else
                    {
                        // something else happened
                        throw e;
                    }
                }

                Thread.Sleep(500); // cooldown  
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
                SetConnected(true);
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
            SetConnected(false);
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
