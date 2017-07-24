using System;
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
            string script = file["Kiiroo"]?["onyx"]?.Value;
            if (string.IsNullOrWhiteSpace(script))
                return null;

            string[] commands = script.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            byte[] mappings = { 0, 25, 50, 75, 99 };

            List<ScriptAction> result = new List<ScriptAction>();
            foreach (string command in commands)
            {
                int commaPosition = command.IndexOf(",", StringComparison.Ordinal);
                string timestampString = command.Substring(0, commaPosition);
                double timestampValue = double.Parse(timestampString, Culture);
                TimeSpan timestamp = TimeSpan.FromSeconds(timestampValue);

                string positionString = command.Substring(commaPosition + 1);
                int positionValue = int.Parse(positionString, Culture);
                byte position = mappings[positionValue];

                result.Add(new FunScriptAction
                {
                    Position = position,
                    TimeStamp = timestamp
                });
            }

            return result;
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
