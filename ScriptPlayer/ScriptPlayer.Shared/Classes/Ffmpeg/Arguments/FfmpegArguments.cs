using System.Collections.Generic;

namespace ScriptPlayer.Shared
{
    public abstract class FfmpegArguments
    {
        public StatusUpdateHandler StatusUpdateHandler;

        public string InputFile { get; set; }

        public abstract string BuildArguments();

        public List<string> TempFiles { get; set; } = new List<string>();
    }
}