using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ScriptPlayer.Shared.Scripts
{
    public class FeelMeRegexLoader : FeelMeLoaderBase
    {
        private const string RegexPattern = "[0-9\\.]+(?<s1>[^0-9\\-])[0-9]+(?<s2>[^0-9\\-])[0-9\\.]+\\<s1>[0-9](\\<s2>[0-9\\.]+\\<s1>[0-9]){4,}";
        private static Regex _regex = new Regex(RegexPattern, RegexOptions.Compiled);

        protected override FeelMeScript GetScriptContent(string content)
        {
            

            Match bestMatch = null;

            var matches = _regex.Matches(content);

            foreach (Match match in matches)
            {
                if (bestMatch == null || match.Value.Length > bestMatch.Value.Length)
                {
                    bestMatch = match;
                }
            }

            if (bestMatch == null)
                return null;

            FeelMeScript result = new FeelMeScript();
            result.String = bestMatch.Value;
            result.PairSeparator = bestMatch.Groups["s2"].Captures[0].Value[0];
            result.ValueSeparator = bestMatch.Groups["s1"].Captures[0].Value[0];

            return result;
        }

        public override List<ScriptFileFormat> GetSupportedFormats()
        {
            return new List<ScriptFileFormat>
            {
                new ScriptFileFormat("FeelMe Meta File (Regex)", "meta", "json")
            };
        }
    }
}