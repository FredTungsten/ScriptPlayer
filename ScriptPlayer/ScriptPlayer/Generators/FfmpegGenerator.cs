using System.IO;
using System.Windows.Threading;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Generators
{
    public abstract class FfmpegGenerator<TSettings> : IGenerator<TSettings, GeneratorEntry> where TSettings : FfmpegGeneratorSettings
    {
        protected string FfmpegExePath { get; }

        protected MainViewModel ViewModel { get; set; }

        protected abstract string ProcessingType { get; }

        protected FfmpegGenerator(MainViewModel viewModel)
        {
            ViewModel = viewModel;
            FfmpegExePath = ViewModel.Settings.FfmpegPath;
        }

        public GeneratorJob CreateJob(TSettings settings)
        {
            return new GeneratorJob<TSettings, GeneratorEntry>(settings, this);
        }

        public bool CheckSkip(TSettings settings)
        {
            return settings.SkipIfExists && File.Exists(settings.OutputFile);
        }

        public GeneratorResult Process(TSettings settings, GeneratorEntry entry)
        {
            entry.State = JobStates.Processing;
            var result = ProcessInternal(settings, entry);

            if (!result.Success)
            {
                entry.DoneType = JobDoneTypes.Failure;
            }

            entry.Update(result.Success ? "Done" : "Failed", 1.0);
            entry.State = JobStates.Done;
            
            return result;
        }

        protected abstract GeneratorResult ProcessInternal(TSettings settings, GeneratorEntry entry);

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