namespace ScriptPlayer.VideoSync
{
    public class AnalysisParameters
    {
        public int MinNegativeSamples { get; set; } = 1;
        public int MinPositiveSamples { get; set; } = 1;
        public int MinBetweenBeats { get; set; } = 1;
        public int MaxPositiveSamples { get; set; } = 20;

        public TimeStampDeterminationMethod Method { get; set; } = TimeStampDeterminationMethod.Center;
    }

    public enum TimeStampDeterminationMethod
    {
        FirstOccurence = 0,
        Center = 1,
        LastOccurence = 2
    }
}