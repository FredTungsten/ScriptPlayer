using System.Collections.Generic;

namespace ScriptPlayer.Shared.Scripts
{
    public class VirtualRealPornScriptLoader : FeelMeLoaderBase
    {
        public override List<ScriptFileFormat> GetSupportedFormats()
        {
            return new List<ScriptFileFormat>
            {
                new ScriptFileFormat("VirtualRealPorn Script", "ini", "txt")
            };
        }

        protected override FeelMeScript GetScriptContent(string content)
        {
            IniFile file = IniFile.FromString(content);
            string script = file["Kiiroo"]?["onyx"]?.Value ?? file["Kiiroo"]?["pearl"]?.Value;

            if (string.IsNullOrWhiteSpace(script))
                return null;

            return FeelMeScript.Ini(script);
        }
    }
}
