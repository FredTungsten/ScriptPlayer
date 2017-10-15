using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptPlayer.Shared.Scripts
{
    public class OneTouchScriptLoader : ScriptLoader
    {
        public override List<ScriptAction> Load(Stream stream)
        {
            List<FunScriptAction> result = new List<FunScriptAction>();

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] parameters = line.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    if (parameters.Length < 10) continue;

                    long timestamp;

                    if (!long.TryParse(parameters[0], out timestamp))
                        continue;

                    //P == Penetration
                    if (parameters[1] != "P")
                        continue;

                    //U,T,B == Up/Top/Both ???
                    //if (parameters[3] != "U")
                    //    continue;

                    byte position;

                    if (parameters[5] == "IN")
                        position = 99;
                    else if (parameters[5] == "OUT")
                        position = 0;
                    else
                        continue;

                    result.Add(new FunScriptAction
                    {
                        Position = position,
                        TimeStamp = TimeSpan.FromMilliseconds(timestamp)
                    });
                }
            }

            return result.Cast<ScriptAction>().ToList();
        }

        public override List<ScriptFileFormat> GetSupportedFormats()
        {
            return new List<ScriptFileFormat>
            {
                new ScriptFileFormat("One Touch Script", "ott")
            };
        }
    }
}
