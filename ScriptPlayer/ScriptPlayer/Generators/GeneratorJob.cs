namespace ScriptPlayer.Generators
{
    public abstract class GeneratorJob
    {
        public abstract void CheckSkip();
        public abstract void Process();
        public abstract GeneratorEntry CreateEntry();
        public abstract void Cancel();
    }

    public class GeneratorJob<TSettings, TEntry> : GeneratorJob where TEntry : GeneratorEntry
    {
        private readonly TSettings _settings;

        private TEntry _entry;
        private bool _skip;

        public GeneratorJob(TSettings settings, IGenerator<TSettings, TEntry> generator)
        {
            _settings = settings;
            Generator = generator;
        }
        
        public IGenerator<TSettings, TEntry> Generator { get; set; }

        public override void CheckSkip()
        {
            _skip = Generator.CheckSkip(_settings);
            if (!_skip) return;

            _entry.Update("Skipped", 1);
            _entry.DoneType = JobDoneTypes.Skipped;
            _entry.State = JobStates.Done;
        }

        public override void Process()
        {
            if(!_skip)
                Generator.Process(_settings, _entry);
        }

        public override GeneratorEntry CreateEntry()
        {
            _entry = Generator.CreateEntry(_settings);
            return _entry;
        }

        public override void Cancel()
        {
            Generator.Cancel();
        }
    }
}