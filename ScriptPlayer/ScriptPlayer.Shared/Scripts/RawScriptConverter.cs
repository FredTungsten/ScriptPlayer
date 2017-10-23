using System;
using System.Collections.Generic;

namespace ScriptPlayer.Shared.Scripts
{
    public static class RawScriptConverter
    {
        public static List<FunScriptAction> Convert(List<RawScriptAction> actions)
        {
            List<FunScriptAction> result = new List<FunScriptAction>();

            byte previousPosition = 0;

            for (int i = 0; i < actions.Count; i++)
            {
                if (i == 0)
                {
                    result.Add(new FunScriptAction
                    {
                        Position = actions[i].Position,
                        TimeStamp = actions[i].TimeStamp
                    });

                    previousPosition = actions[i].Position;
                }
                else
                {
                    TimeSpan elapsed = actions[i].TimeStamp - actions[i - 1].TimeStamp;

                    if (actions[i - 1].Position == actions[i].Position)
                    {
                        result.Add(new FunScriptAction
                        {
                            TimeStamp = actions[i].TimeStamp,
                            Position = previousPosition
                        });
                    }
                    else
                    {
                        byte newPosition = PredictPosition(previousPosition, actions[i - 1].Position, actions[i - 1].Speed, elapsed);

                        result.Add(new FunScriptAction
                        {
                            TimeStamp = actions[i].TimeStamp,
                            Position = newPosition
                        });

                        previousPosition = newPosition;
                    }
                }
            }

            return FilterEquals(result);
        }

        private static List<FunScriptAction> FilterEquals(List<FunScriptAction> actions)
        {
            List<FunScriptAction> result = new List<FunScriptAction>();

            for(int i = 0; i < actions.Count; i++)
            {
                if (i == 0 || i == actions.Count - 1)
                {
                    result.Add(actions[i]);
                }
                else
                {
                    if (actions[i].Position == actions[i - 1].Position &&
                        actions[i].Position == actions[i + 1].Position)
                        continue;

                    result.Add(actions[i]);
                }
            }

            return result;
        }

        private static byte PredictPosition(byte previousPosition, byte position, byte speed, TimeSpan elapsed)
        {
            byte distance = SpeedPredictor.PredictDistance(speed, elapsed);
            int direction = Math.Sign(position - previousPosition);
            byte newPosition = ClampPosition(previousPosition + direction * distance);
            return newPosition;
        }

        private static byte ClampPosition(int value)
        {
            if (value > 99) return 99;
            if (value < 0) return 0;

            return (byte)value;
        }
    }
}