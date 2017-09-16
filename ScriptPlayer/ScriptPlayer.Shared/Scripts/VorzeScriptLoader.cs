using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScriptPlayer.Shared.Scripts
{
    public class VorzeScriptToFunscriptLoader : VorzeScriptLoader
    {
        public override List<ScriptAction> Load(Stream stream)
        {
            List<VorzeScriptAction> vorzeActions = base.Load(stream).Cast<VorzeScriptAction>().ToList();
            return VorzeToFunscriptConverter.Convert(vorzeActions).Cast<ScriptAction>().ToList();
        }
    }

    public class VorzeScriptLoader : ScriptLoader
    {
        public override List<ScriptAction> Load(Stream stream)
        {
            List<ScriptAction> actions = new List<ScriptAction>();

            using (StreamReader reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;

                    int[] parameters = line.Split(new []{','},StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
                    actions.Add(new VorzeScriptAction
                    {
                        TimeStamp = TimeSpan.FromMilliseconds(100.0 * parameters[0]),
                        Action = parameters[1],
                        Parameter = parameters[2]
                    });
                }
            }

            return actions;
        }

        public override List<ScriptFileFormat> GetSupportedFormats()
        {
            return new List<ScriptFileFormat>
            {
                new ScriptFileFormat("Vorze Script (beta)", "csv")
            };
        }
    }
}