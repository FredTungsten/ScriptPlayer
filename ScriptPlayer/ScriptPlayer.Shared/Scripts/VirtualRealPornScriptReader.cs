using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptPlayer.Shared.Scripts
{
    public class VirtualRealPornScriptLoader : ScriptLoader
    {
        public override List<ScriptAction> Load(Stream stream)
        {
            IniFile file = IniFile.FromStream(stream);
            string script = file["Kiiroo"]?["onyx"]?.Value ?? file["Kiiroo"]?["pearl"]?.Value;
            
            if (string.IsNullOrWhiteSpace(script))
                return null;

            var kiirooScript = KiirooScriptConverter.ParseVrpScript(script);
            var rawScript = KiirooScriptConverter.Convert(kiirooScript);
            var funscript = RawScriptConverter.Convert(rawScript);
            return funscript.Cast<ScriptAction>().ToList();
        }

        public override List<ScriptFileFormat> GetSupportedFormats()
        {
            return new List<ScriptFileFormat>
            {
                new ScriptFileFormat("VirtualRealPorn Script", "ini", "txt")
            };
        }
    }
}
