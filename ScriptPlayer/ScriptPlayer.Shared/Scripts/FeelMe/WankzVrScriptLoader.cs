using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ScriptPlayer.Shared.Scripts
{
    public class WankzVrScriptLoader : FeelMeJsonLoaderBase
    {
        public override List<ScriptFileFormat> GetSupportedFormats()
        {
            return new List<ScriptFileFormat>
            {
                new ScriptFileFormat("WankzVR Meta File", "meta", "json")
            };
        }

        protected override JToken GetScriptNode(JToken file)
        {
            return file["text"];
        }
    }
}