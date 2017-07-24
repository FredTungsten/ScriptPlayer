using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ScriptPlayer.Shared.Scripts
{
    public abstract class ScriptLoader
    {
        protected CultureInfo Culture = CultureInfo.InvariantCulture;

        public abstract List<ScriptAction> Load(Stream stream);

        public abstract List<ScriptFileFormat> GetSupportedFormats();

        public List<ScriptAction> Load(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                return Load(stream);
        }
    }
}
