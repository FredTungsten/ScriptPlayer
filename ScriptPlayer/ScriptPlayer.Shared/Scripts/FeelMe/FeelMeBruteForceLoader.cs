using System.Collections.Generic;

namespace ScriptPlayer.Shared.Scripts
{
    public class FeelMeBruteForceLoader : FeelMeLoaderBase
    {
        public override List<ScriptFileFormat> GetSupportedFormats()
        {
            return new List<ScriptFileFormat>
            {
                new ScriptFileFormat("FeelMe Meta File (Brute Force)", "meta", "json")
            };
        }

        protected override FeelMeScript GetScriptContent(string inputString)
        {
            FeelMeScript bestResult = null;

            List<char> possibleSeparators = new List<char>();

            for (int i = 0; i < inputString.Length - 1; i++)
            {
                possibleSeparators.Clear();

                if (inputString[i] > '9' || inputString[i] < '0')
                    continue;

                int j;

                for (j = i; j < inputString.Length; j++)
                {
                    if (inputString[j] >= '0' && inputString[j] <= '9') continue;
                    if (inputString[j] == '.' || char.IsWhiteSpace(inputString[j])) continue;

                    if (possibleSeparators.Contains(inputString[j])) continue;
                    if (possibleSeparators.Count < 2)
                    {
                        possibleSeparators.Add(inputString[j]);
                        continue;
                    }

                    break;
                }

                if (possibleSeparators.Count != 2) continue;

                FeelMeScript result = new FeelMeScript
                {
                    PairSeparator = possibleSeparators[1],
                    ValueSeparator = possibleSeparators[0],
                    String = inputString.Substring(i, j - i)
                };

                i = j;

                if (bestResult == null || bestResult.String.Length < result.String.Length)
                    bestResult = result;
            }

            return bestResult;
        }
    }
}