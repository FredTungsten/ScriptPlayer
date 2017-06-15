using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ScriptPlayer.Shared.Scripts
{
    public class RawScriptLoader : ScriptLoader
    {
        public override List<ScriptAction> Load(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                string content = reader.ReadToEnd();
                var actions = JsonConvert.DeserializeObject<List<RawScriptAction>>(content);
                return actions.Cast<ScriptAction>().ToList();
            }
        }

        public override List<ScriptFileFormat> GetSupportedFormats()
        {
            return new List<ScriptFileFormat>
            {
                new ScriptFileFormat("Raw Script", "launch", "json")
            };
        }
    }
}