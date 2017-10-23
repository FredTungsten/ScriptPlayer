using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ScriptPlayer.Shared.Scripts
{
    public abstract class FeelMeLoaderBase : ScriptLoader
    {
        public override List<ScriptAction> Load(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(false)))
            {
                string content = reader.ReadToEnd();

                FeelMeScript script = GetScriptContent(content);

                string scriptContent = TrimNonNumberCharacters(script.String);
                var kiirooScript = KiirooScriptConverter.Parse(scriptContent, script.PairSeparator, script.ValueSeparator);
                var rawScript = KiirooScriptConverter.Convert(kiirooScript);
                var funscript = RawScriptConverter.Convert(rawScript);
                return funscript.Cast<ScriptAction>().ToList();
            }
        }

        private static string TrimNonNumberCharacters(string script)
        {
            int begin = -1;
            int end = -1;

            for (int i = 0; i < script.Length; i++)
            {
                if (script[i] < '0' || script[i] > '9') continue;
                begin = i;
                break;
            }

            for (int i = script.Length - 1; i >= 0; i--)
            {
                if (script[i] < '0' || script[i] > '9') continue;
                end = i;
                break;
            }

            if (begin < 0 || end < 0)
                return script;

            return script.Substring(begin, end - begin + 1);
        }

        protected abstract FeelMeScript GetScriptContent(string content);
    }
}