namespace ScriptPlayer.Generators
{
    public interface IGenerator<in TSettings, TEntry> where TEntry : GeneratorEntry
    {
        GeneratorJob CreateJob(TSettings settings);

        bool CheckSkip(TSettings settings);

        void Process(TSettings settings, TEntry entry);

        TEntry CreateEntry(TSettings settings);

        void Cancel();
    }
}