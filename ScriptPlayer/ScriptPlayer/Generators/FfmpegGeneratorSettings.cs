using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Generators
{
    public abstract class FfmpegGeneratorSettings
    {
        public string VideoFile { get; set; }

        public string OutputFile { get; set; }

        public JitRenamer RenameBeforeExecute { get; set; }

        public bool SkipIfExists { get; set; }
    }
}