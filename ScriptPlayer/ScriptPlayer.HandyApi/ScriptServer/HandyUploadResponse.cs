using Newtonsoft.Json;
using ScriptPlayer.HandyApi.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptPlayer.HandyApi.ScriptServer
{
    internal class HandyUploadResponse
    {
        public bool success { get; set; }
        public bool? converted { get; set; }
        public string filename { get; set; }
        public string info { get; set; }
        public string orginalfile { get; set; }
        public int size { get; set; }
        public string url { get; set; }
        public string error { get; set; }
    }

    public class HandyHostingUploadResponse
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("error")]
        public DeviceError Error { get; set; }
    }

}
