using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ScriptPlayer.Shared.Scripts
{
    public class FeelMeScriptLoader : ScriptLoader
    {
        public override List<ScriptAction> Load(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(false)))
            {
                string content = reader.ReadToEnd();
                var file = JsonConvert.DeserializeObject<FeelMeMetaFile>(content);

                string script = file.Text.Trim('{', '}', ' ', '\t', '\r', '\n');
                var kiirooScript = KiirooScriptConverter.ParseFeelMe(script);
                var rawScript = KiirooScriptConverter.Convert(kiirooScript);
                var funscript = RawScriptConverter.Convert(rawScript);
                return funscript.Cast<ScriptAction>().ToList();
            }
        }

        public override List<ScriptFileFormat> GetSupportedFormats()
        {
            return new List<ScriptFileFormat>
            {
                new ScriptFileFormat("FeelMe Meta File", "meta", "json")
            };
        }
    }

    public class FeelMeMetaFile
    {
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [JsonProperty(PropertyName = "created")]
        public DateTime CreationTime { get; set; }

        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "session_id")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "video_external_id")]
        public string ExternalVideoId { get; set; }

        [JsonProperty(PropertyName = "video")]
        public FeelMeVideo Video { get; set; }

        public FeelMeMetaFile()
        {
            
        }
    }

    public class FeelMeVideo
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "created")]
        public DateTime CreationTime { get; set; }

        [JsonProperty(PropertyName = "external_id")]
        public string ExternalId { get; set; }

        [JsonProperty(PropertyName = "subtitles_count")]
        public int SubtitlesCount { get; set; }
    }
}
