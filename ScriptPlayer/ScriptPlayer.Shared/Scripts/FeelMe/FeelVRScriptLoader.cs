using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ScriptPlayer.Shared.Scripts
{
    public class FeelVrScriptLoader : FeelMeJsonLoaderBase
    {
        public override List<ScriptFileFormat> GetSupportedFormats()
        {
            return new List<ScriptFileFormat>
            {
                new ScriptFileFormat("FeelVR Meta File", "meta", "json")
            };
        }

        protected override JToken GetScriptNode(JToken file)
        {
            return file["subs"]["text"];
        }
    }
}
