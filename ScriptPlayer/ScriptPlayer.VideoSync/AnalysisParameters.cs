namespace ScriptPlayer.VideoSync
{
    public class AnalysisParameters
    {
        public int MinNegativeSamples { get; set; } = 1;
        public int MinPositiveSamples { get; set; } = 1;
        public int MinBetweenBeats { get; set; } = 1;

        public int MaxPositiveSamples { get; set; } = 20;
    }
}