using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScriptPlayer.Shared.Scripts
{
    public class BeatFileLoader : ScriptLoader
    {
        public override List<ScriptAction> Load(Stream stream)
        {
            BeatCollection beats = BeatCollection.Load(stream);

            return beats.Select(beat => new BeatScriptAction
            {
                TimeStamp = beat
            }).Cast<ScriptAction>().ToList();
        }

        public override List<ScriptFileFormat> GetSupportedFormats()
        {
            return new List<ScriptFileFormat>
            {
                new ScriptFileFormat("Beat File", "txt", "beats")
            };
        }
    }
}