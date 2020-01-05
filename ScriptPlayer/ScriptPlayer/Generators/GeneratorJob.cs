namespace ScriptPlayer.Generators
{
    public abstract class GeneratorJob : IGeneratorJobWithFfmpegSettings
    {
        public abstract GeneratorEntry Entry { get; }
        public abstract void CheckSkip();
        public abstract GeneratorResult Process();
        public abstract GeneratorEntry CreateEntry();
        public abstract void Cancel();

        public abstract bool HasIdenticalSettings(GeneratorJob job);
        public abstract bool HasIdenticalSettings(FfmpegGeneratorSettings settings);

        public abstract string VideoFileName { get; }
        public abstract FfmpegGeneratorSettings GetSettings();
    }

    public interface IGeneratorJobWithFfmpegSettings
    {
        string VideoFileName { get; }
        FfmpegGeneratorSettings GetSettings();
    }

    public class GeneratorJob<TSettings, TEntry> : GeneratorJob, IGeneratorJobWithFfmpegSettings where TEntry : GeneratorEntry where TSettings : FfmpegGeneratorSettings
    {
        private readonly TSettings _settings;

        private TEntry _entry;
        private bool _skip;
        private bool _cancelled;

        public GeneratorJob(TSettings settings, IGenerator<TSettings, TEntry> generator)
        {
            _settings = settings;
            Generator = generator;
        }
        
        public IGenerator<TSettings, TEntry> Generator { get; set; }

        public override GeneratorEntry Entry => _entry;

        public override void CheckSkip()
        {
            _skip = Generator.CheckSkip(_settings);
            if (!_skip) return;

            _entry.Update("Skipped", 1);
            _entry.DoneType = JobDoneTypes.Skipped;
            _entry.State = JobStates.Done;
        }

        public override GeneratorResult Process()
        {
            if (!_skip && !_cancelled)
            {
                if (_settings.RenameBeforeExecute != null)
                {
                    if(!_settings.RenameBeforeExecute.RenameNow())
                        return GeneratorResult.Failed();
                }

                return Generator.Process(_settings, _entry);
            }

            return GeneratorResult.Failed();
        }

        public override GeneratorEntry CreateEntry()
        {
            _entry = Generator.CreateEntry(_settings);
            _entry.Job = this;
            return _entry;
        }

        public override void Cancel()
        {
            if (_cancelled)
                return;

            _entry.Update("Cancelled", 1);
            _entry.DoneType = JobDoneTypes.Cancelled;
            _entry.State = JobStates.Done;

            _cancelled = true;
            Generator.Cancel();
        }

        public override bool HasIdenticalSettings(GeneratorJob job)
        {
            if (!(job is IGeneratorJobWithFfmpegSettings jobWithSettings))
                return false;

            return _settings.IsIdenticalTo(jobWithSettings.GetSettings());
        }

        public override bool HasIdenticalSettings(FfmpegGeneratorSettings settings)
        {
            if ((_settings == null) != (settings == null))
                return false;

            if (_settings == null)
                return true;

            return _settings.IsIdenticalTo(settings);
        }

        public override string VideoFileName => _settings.VideoFile;

        public override FfmpegGeneratorSettings GetSettings()
        {
            return _settings;
        }
    }
}