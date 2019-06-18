using System.Collections.Generic;
using System.IO;

namespace ScriptPlayer.Shared
{
    public class FfmpegConsoleWrapper : ConsoleWrapper
    {
        private readonly List<string> _tempfiles = new List<string>();

        public FfmpegConsoleWrapper(FfmpegArguments arguments, string ffmpegExe)
        {
            Executable = ffmpegExe;
            FfmpegArguments = arguments;
            Arguments = arguments.BuildArguments();
            _tempfiles.AddRange(arguments.TempFiles);
        }

        protected FfmpegArguments FfmpegArguments { get; }

        public void Cancel()
        {
            Input("q");
        }

        protected void UpdateProgress(double progress)
        {
            FfmpegArguments.StatusUpdateHandler?.Invoke(progress);
        }

        protected void AddTempFile(string fileName)
        {
            _tempfiles.Add(fileName);
        }

        protected override void AfterExecute(int exitCode)
        {
            base.AfterExecute(exitCode);

            foreach (string file in _tempfiles)
            {
                try
                {
                    if(File.Exists(file))
                        File.Delete(file);
                }
                catch { }
            }

            _tempfiles.Clear();
        }
    }
}