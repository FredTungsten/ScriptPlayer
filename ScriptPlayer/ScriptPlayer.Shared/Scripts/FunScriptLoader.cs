using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace ScriptPlayer.Shared.Scripts
{
    public class FunScriptLoader : ScriptLoader
    {
        public override List<ScriptAction> Load(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(false)))
            {
                string content = reader.ReadToEnd();
                var file = JsonConvert.DeserializeObject<FunScriptFile>(content);

                if (file.Inverted)
                    file.Actions.ForEach(a => a.Position = (byte) (99- a.Position));

                var actions = file.Actions.Cast<ScriptAction>().Where(a => a.TimeStamp >= TimeSpan.Zero).ToList();
                return actions;
            }
        }

        public override List<ScriptFileFormat> GetSupportedFormats()
        {
            return new List<ScriptFileFormat>
            {
                new ScriptFileFormat("Fun Script", "funscript", "json")
            };
        }
    }
}
