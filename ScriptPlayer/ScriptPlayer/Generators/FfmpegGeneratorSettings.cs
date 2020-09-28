using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Generators
{
    public abstract class FfmpegGeneratorSettings
    {
        public string VideoFile { get; set; }

        public string OutputFile { get; set; }

        public JitRenamer RenameBeforeExecute { get; set; }

        public bool SkipIfExists { get; set; }

        public bool ClipLeft { get; set; }

        public virtual bool IsIdenticalTo(FfmpegGeneratorSettings settings)
        {
            if (VideoFile != settings.VideoFile) return false;
            if (OutputFile != settings.OutputFile) return false;
            if (RenameBeforeExecute != settings.RenameBeforeExecute) return false;
            if (SkipIfExists != settings.SkipIfExists) return false;
            if (ClipLeft != settings.ClipLeft) return false;

            return true;
        }
    }
}