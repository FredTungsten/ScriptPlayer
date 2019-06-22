namespace ScriptPlayer.ViewModels
{
    /// <summary>
    /// Determines how the progress time is displayed
    /// </summary>
    public enum TimeDisplayMode
    {
        /// <summary>
        /// Original Video Position regardless of script
        /// </summary>
        Original,

        /// <summary>
        /// First Command to Last Command
        /// </summary>
        ContentAndGaps,

        /// <summary>
        /// Only Content in Sections/Chapters
        /// </summary>
        ContentOnly
    }
}