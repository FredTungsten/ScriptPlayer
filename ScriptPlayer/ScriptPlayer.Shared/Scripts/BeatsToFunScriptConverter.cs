using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptPlayer.Shared.Scripts
{
    public enum ConversionMode
    {
        /// <summary>
        /// One beat up, next beat down
        /// </summary>
        UpOrDown,
        UpDownFast,
        DownFast,
        DownCenter,
        UpFast,
        UpCenter
    }

    public static class BeatsToFunScriptConverter
    {
        public static List<FunScriptAction> Convert(IEnumerable<TimeSpan> timestamps, ConversionMode mode)
        {
            var beats = timestamps.ToList();
            var actions = new List<FunScriptAction>();

            TimeSpan previousTimeStamp = TimeSpan.FromDays(-1);
            TimeSpan previousDuration = TimeSpan.FromDays(1);
            
            TimeSpan centerLimit;

            bool up = true;

            byte positionDown;
            byte positionUp;

            switch (mode)
            {
                case ConversionMode.UpOrDown:
                    centerLimit = TimeSpan.Zero;
                    positionDown = 5;
                    positionUp = 95;
                    break;
                case ConversionMode.UpDownFast:
                    centerLimit = TimeSpan.Zero;
                    positionDown = 5;
                    positionUp = 95;
                    break;
                case ConversionMode.DownFast:
                    centerLimit = TimeSpan.FromMilliseconds(180);
                    positionDown = 95;
                    positionUp = 5;
                    break;
                case ConversionMode.DownCenter:
                    centerLimit = TimeSpan.FromMilliseconds(2000);
                    positionDown = 95;
                    positionUp = 5;
                    break;
                case ConversionMode.UpFast:
                    centerLimit = TimeSpan.FromMilliseconds(180);
                    positionDown = 5;
                    positionUp = 95;
                    break;
                case ConversionMode.UpCenter:
                    centerLimit = TimeSpan.FromMilliseconds(2000);
                    positionDown = 5;
                    positionUp = 95;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            for (int index = 0; index < beats.Count; index++)
            {
                TimeSpan timestamp = beats[index];
                up ^= true;

                switch (mode)
                {
                    case ConversionMode.UpDownFast:
                    {
                        if (index > 0)
                        {
                            TimeSpan duration = timestamp - previousTimeStamp;

                            if (duration > previousDuration)
                            {
                                actions.Add(new FunScriptAction
                                {
                                    Position = up ? positionUp : positionDown,
                                    TimeStamp = timestamp - (duration - previousDuration)
                                });
                            }
                        }

                        actions.Add(new FunScriptAction
                        {
                            Position = up ? positionUp : positionDown,
                            TimeStamp = timestamp
                        });
                        break;
                    }
                    case ConversionMode.UpOrDown:
                    {
                        actions.Add(new FunScriptAction
                        {
                            Position = up ? positionUp : positionDown,
                            TimeStamp = timestamp
                        });
                        break;
                    }
                    case ConversionMode.UpCenter:
                    case ConversionMode.DownCenter:
                    case ConversionMode.UpFast:
                    case ConversionMode.DownFast:
                    {
                        if (previousTimeStamp != TimeSpan.MinValue)
                        {
                            if (timestamp - previousTimeStamp >= centerLimit.Multiply(2))
                            {
                                actions.Add(new FunScriptAction
                                {
                                    Position = positionDown,
                                    TimeStamp = previousTimeStamp + centerLimit
                                });

                                actions.Add(new FunScriptAction
                                {
                                    Position = positionDown,
                                    TimeStamp = timestamp - centerLimit
                                });
                            }
                            else
                            {
                                actions.Add(new FunScriptAction
                                {
                                    Position = positionDown,
                                    TimeStamp = (previousTimeStamp + timestamp).Divide(2)
                                });
                            }
                        }
                    }

                        actions.Add(new FunScriptAction
                        {
                            Position = positionUp,
                            TimeStamp = timestamp
                        });

                        break;
                }

                previousDuration = timestamp - previousTimeStamp;

                previousTimeStamp = timestamp;
            }

            return actions;
        }
    }
}
