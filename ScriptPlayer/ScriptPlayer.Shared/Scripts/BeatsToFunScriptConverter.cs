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
        DownFastSlow,
        DownSlowFast,
        UpFast,
        UpCenter,
        UpFastSlow,
        UpSlowFast,
        Custom
    }

    public class ConversionSettings
    {
        public ConversionMode Mode { get; set; }
        public byte Min { get; set; }
        public byte Max { get; set; }
        public PositionCollection CustomPositions { get; set; }
    }

    public static class BeatsToFunScriptConverter
    {
        public static List<FunScriptAction> Convert(IEnumerable<TimeSpan> timestamps, ConversionSettings settings, int startIndex = 0)
        {
            var beats = timestamps.OrderBy(a => a).ToList();
            var actions = new List<FunScriptAction>();

            TimeSpan previousTimeStamp = TimeSpan.FromDays(-1);
            TimeSpan previousDuration = TimeSpan.FromDays(1);
            
            TimeSpan centerLimit;

            bool up = startIndex % 2 == 0;

            byte positionDown;
            byte positionUp;
            bool slowFirst = false;

            TimeSpan fastLimit = TimeSpan.FromMilliseconds(180);
            TimeSpan slowLimit = TimeSpan.FromMilliseconds(2000);

            List<Tuple<double, byte>> relativeCustomPositions = new List<Tuple<double, byte>>();
            bool flips = false;

            switch (settings.Mode)
            {
                case ConversionMode.UpOrDown:
                    centerLimit = TimeSpan.Zero;
                    positionDown = settings.Min;
                    positionUp = settings.Max;
                    break;
                case ConversionMode.UpDownFast:
                    centerLimit = TimeSpan.Zero;
                    positionDown = settings.Min;
                    positionUp = settings.Max;
                    break;
                case ConversionMode.DownFast:
                    centerLimit = fastLimit;
                    positionDown = settings.Max;
                    positionUp = settings.Min;
                    break;
                case ConversionMode.DownCenter:
                    centerLimit = slowLimit;
                    positionDown = settings.Max;
                    positionUp = settings.Min;
                    break;
                case ConversionMode.DownFastSlow:
                    centerLimit = fastLimit;
                    positionDown = settings.Max;
                    positionUp = settings.Min;
                    break;
                case ConversionMode.DownSlowFast:
                    centerLimit = fastLimit;
                    positionDown = settings.Max;
                    positionUp = settings.Min;
                    slowFirst = true;
                    break;
                case ConversionMode.UpFast:
                    centerLimit = fastLimit;
                    positionDown = settings.Min;
                    positionUp = settings.Max;
                    break;
                case ConversionMode.UpFastSlow:
                    centerLimit = fastLimit;
                    positionDown = settings.Min;
                    positionUp = settings.Max;
                    break;
                case ConversionMode.UpSlowFast:
                    centerLimit = fastLimit;
                    positionDown = settings.Min;
                    positionUp = settings.Max;
                    slowFirst = true;
                    break;
                case ConversionMode.UpCenter:
                    centerLimit = slowLimit;
                    positionDown = settings.Min;
                    positionUp = settings.Max;
                    break;
                case ConversionMode.Custom:
                    centerLimit = TimeSpan.Zero;
                    positionDown = settings.Min;
                    positionUp = settings.Max;

                    TimeSpan customDuration = settings.CustomPositions.Last().TimeStamp -
                                              settings.CustomPositions.First().TimeStamp;

                    flips = settings.CustomPositions.First().Position != settings.CustomPositions.Last().Position;

                    foreach (TimedPosition pos in settings.CustomPositions)
                    {
                        double relativePosition =
                            (pos.TimeStamp - settings.CustomPositions.First().TimeStamp).Divide(customDuration);

                        relativeCustomPositions.Add(new Tuple<double, byte>(relativePosition, pos.Position));
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(settings.Mode), settings.Mode, null);
            }

            

            
            for (int index = 0; index < beats.Count; index++)
            {
                TimeSpan timestamp = beats[index];
                up ^= true;

                switch (settings.Mode)
                {
                    case ConversionMode.Custom:
                    {
                        if (index > 0)
                        {
                            TimeSpan duration = timestamp - previousTimeStamp;

                            for (int x = 0; x < relativeCustomPositions.Count; x++)
                            {
                                if (x == relativeCustomPositions.Count - 1 && index != beats.Count - 1)
                                    continue;

                                actions.Add(new FunScriptAction
                                {
                                    Position = (byte)((flips && !up) ? 99 - relativeCustomPositions[x].Item2 : relativeCustomPositions[x].Item2),
                                    TimeStamp = previousTimeStamp + duration.Multiply(relativeCustomPositions[x].Item1)
                                });
                            }
                        }

                        break;
                    }
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

                        actions.Add(new FunScriptAction
                        {
                            Position = positionUp,
                            TimeStamp = timestamp
                        });

                        break;
                    }
                    case ConversionMode.UpFastSlow:
                    case ConversionMode.DownFastSlow:
                    case ConversionMode.UpSlowFast:
                    case ConversionMode.DownSlowFast:
                    {
                        if (previousTimeStamp != TimeSpan.MinValue)
                        {
                            if (timestamp - previousTimeStamp >= centerLimit.Multiply(2))
                            {
                                if(slowFirst)
                                {
                                    actions.Add(new FunScriptAction
                                    {
                                        Position = positionDown,
                                        TimeStamp = previousTimeStamp + centerLimit
                                    });
                                }
                                else
                                {
                                    actions.Add(new FunScriptAction
                                    {
                                        Position = positionDown,
                                        TimeStamp = timestamp - centerLimit
                                    });
                                }
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

                        actions.Add(new FunScriptAction
                        {
                            Position = positionUp,
                            TimeStamp = timestamp
                        });

                        break;
                        }
                }

                previousDuration = timestamp - previousTimeStamp;

                previousTimeStamp = timestamp;
            }

            return actions;
        }
    }
}
