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

            TimeSpan previous = TimeSpan.MinValue;
            TimeSpan centerLimit = TimeSpan.MaxValue;

            bool up = true;

            byte positionDown;
            byte positionUp;

            switch (mode)
            {
                case ConversionMode.UpOrDown:
                    centerLimit = TimeSpan.Zero;
                    positionDown = 95;
                    positionUp = 5;
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

            foreach (TimeSpan timestamp in beats)
            {
                switch (mode)
                {
                    case ConversionMode.UpOrDown:
                        {
                            up ^= true;

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
                            if (previous != TimeSpan.MinValue)
                            {
                                if (timestamp - previous >= centerLimit.Multiply(2))
                                {
                                    actions.Add(new FunScriptAction
                                    {
                                        Position = positionDown,
                                        TimeStamp = previous + centerLimit
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
                                        TimeStamp = (previous + timestamp).Divide(2)
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

                previous = timestamp;
            }

            return actions;
        }
    }
}
