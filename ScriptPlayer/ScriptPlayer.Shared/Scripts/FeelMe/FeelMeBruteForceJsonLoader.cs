using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ScriptPlayer.Shared.Scripts
{
    public class FeelMeBruteForceJsonLoader : FeelMeJsonLoaderBase
    {
        public override List<ScriptFileFormat> GetSupportedFormats()
        {
            return new List<ScriptFileFormat>
            {
                new ScriptFileFormat("FeelMe Meta File (Json Brute Force)", "meta", "json")
            };
        }

        protected override JToken GetScriptNode(JToken file)
        {
            return file.SelectToken("$..['text']");
        }
    }
}