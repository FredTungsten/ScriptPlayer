using System;
using System.Collections.Generic;

namespace ScriptPlayer.Shared.Scripts
{
    public static class KiirooScriptConverter
    {
        public static List<RawScriptAction> Convert(List<KiirooScriptAction> actions)
        {
            List<RawScriptAction> result = new List<RawScriptAction>();

            double previousSpeed = 0.5;

            for (int i = 0; i < actions.Count; i++)
            {
                TimeSpan elapsed;

                if (i > 0)
                    elapsed = actions[i].TimeStamp - actions[i - 1].TimeStamp;
                else
                    elapsed = TimeSpan.MaxValue;

                double elapsedSeconds = elapsed.TotalSeconds;
                double rawSpeed;

                if (elapsedSeconds >= 2.0)
                    rawSpeed = 0.5;
                else if (elapsedSeconds >= 1.0)
                    rawSpeed = 0.2;
                else
                    rawSpeed = ClampSpeed(1.0 - elapsedSeconds * 1.1);

                double newSpeed;


                //WTF? Seriously?

                if (rawSpeed > previousSpeed)
                    newSpeed = previousSpeed + (rawSpeed - previousSpeed) / 6.0;
                else
                    newSpeed = previousSpeed - rawSpeed / 2.0; 

                previousSpeed = ClampSpeed(newSpeed);

                byte speed = LerpSpeed(newSpeed);
                byte position = LerpValue(actions[i].Value);

                result.Add(new RawScriptAction
                {
                    Position = position,
                    Speed = speed,
                    TimeStamp = actions[i].TimeStamp
                });
            }

            return result;
        }

        private static byte LerpValue(int value)
        {
            return (byte) (value > 2 ? 5 : 95);
        }

        private static byte LerpSpeed(double value)
        {
            int scaledValue = (byte)Math.Round(99.0 * value);

            if (scaledValue > 99) return 99;
            if (scaledValue < 20) return 20;

            return (byte)scaledValue;
        }

        private static int ClampValue(int value)
        {
            if (value < 0) return 0;
            if (value > 4) return 4;

            return value;
        }

        private static double ClampSpeed(double d)
        {
            if (d < 0.2) return 0.2;
            if (d > 1) return 1;

            return d;
        }

        public static List<KiirooScriptAction> Parse(string script, char pairDelimiter, char valueDelimiter)
        {
            List<KiirooScriptAction> result = new List<KiirooScriptAction>();

            string[] commands = script.Split(new[] { pairDelimiter }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string command in commands)
            {
                int commaPosition = command.IndexOf(valueDelimiter+"", StringComparison.Ordinal);
                string timestampString = command.Substring(0, commaPosition);
                double timestampValue = double.Parse(timestampString, ScriptLoader.Culture);
                TimeSpan timestamp = TimeSpan.FromSeconds(timestampValue);

                string valueString = command.Substring(commaPosition + 1);
                int value = ClampValue(int.Parse(valueString, ScriptLoader.Culture));

                result.Add(new KiirooScriptAction
                {
                    Value = value,
                    TimeStamp = timestamp
                });
            }

            return result;
        }
    }

    public class KiirooScriptAction : ScriptAction
    {
        public int Value { get; set; }
    }
}