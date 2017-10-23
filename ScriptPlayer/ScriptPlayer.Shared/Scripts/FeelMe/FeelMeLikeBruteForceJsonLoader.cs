using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ScriptPlayer.Shared.Scripts
{
    public class FeelMeLikeBruteForceJsonLoader : FeelMeJsonLoaderBase
    {
        public override List<ScriptFileFormat> GetSupportedFormats()
        {
            return new List<ScriptFileFormat>
            {
                new ScriptFileFormat("FeelMe Meta File (Unknown Json)", "meta", "json")
            };
        }

        protected override JToken GetScriptNode(JToken file)
        {
            var tokens = file.SelectTokens("$..*");

            var possibleTokens = tokens.Where(t => !t.HasValues).ToList();
            int longestToken = possibleTokens.Max(t => t.ToString().Length);

            return possibleTokens.First(t => t.ToString().Length == longestToken);
        }
    }
}