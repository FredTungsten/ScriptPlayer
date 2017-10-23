namespace ScriptPlayer.Shared.Scripts
{
    public class FeelMeScript
    {
        /// <summary>
        /// The character separating value-pairs
        /// </summary>
        public char PairSeparator { get; set; }

        /// <summary>
        /// The character separating each pair of values
        /// </summary>
        public char ValueSeparator { get; set; }

        /// <summary>
        /// The un-processed script
        /// </summary>
        public string String { get; set; }

        public static FeelMeScript Json(string jsonString)
        {
            return new FeelMeScript
            {
                String = jsonString,
                PairSeparator = ',',
                ValueSeparator = ':'
            };
        }

        public static FeelMeScript Ini(string valueString)
        {
            return new FeelMeScript
            {
                String = valueString,
                PairSeparator = ';',
                ValueSeparator = ','
            };
        }
    }
}