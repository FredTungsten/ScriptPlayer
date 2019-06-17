namespace ScriptPlayer.Shared
{
    public abstract class FfmpegWrapper : ConsoleWrapper
    {
        protected FfmpegWrapper(string ffmpegExe)
        {
            Executable = ffmpegExe;
        }

        protected abstract void SetArguments();

        public string VideoFile { get; set; }

        public void Cancel()
        {
            Input("q");
        }

        protected override void BeforeExecute()
        {
            base.BeforeExecute();

            SetArguments();
        }
    }
}