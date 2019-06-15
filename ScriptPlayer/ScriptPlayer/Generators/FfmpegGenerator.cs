using System.IO;
using System.Windows.Threading;

namespace ScriptPlayer.Generators
{
    public abstract class FfmpegGenerator<TSettings> : IGenerator<TSettings, GeneratorEntry> 
        where TSettings : FfmpegGeneratorSettings
    {
        protected string FfmpegExePath { get; }

        protected abstract string ProcessingType { get; }

        protected FfmpegGenerator(string ffmpegExePath)
        {
            FfmpegExePath = ffmpegExePath;
        }

        public GeneratorJob CreateJob(TSettings settings)
        {
            return new GeneratorJob<TSettings, GeneratorEntry>(settings, this);
        }

        public bool CheckSkip(TSettings settings)
        {
            return settings.SkipIfExists && File.Exists(settings.OutputFile);
        }

        public void Process(TSettings settings, GeneratorEntry entry)
        {
            entry.State = JobStates.Processing;
            ProcessInternal(settings, entry);
            entry.State = JobStates.Done;
        }

        protected abstract void ProcessInternal(TSettings settings, GeneratorEntry entry);

        public abstract void Cancel();

        public virtual GeneratorEntry CreateEntry(TSettings settings)
        {
            GeneratorEntry entry = new GeneratorEntry(Dispatcher.CurrentDispatcher)
            {
                Type = ProcessingType,
                Filename = settings.VideoFile
            };

            return entry;
        }
    }
}