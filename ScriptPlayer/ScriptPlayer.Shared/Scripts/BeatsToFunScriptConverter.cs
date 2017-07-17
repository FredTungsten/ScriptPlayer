using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptPlayer.Shared.Scripts
{
    public static class BeatsToFunScriptConverter
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

        public static List<FunScriptAction> Convert(IEnumerable<TimeSpan> timestamps, ConversionMode mode)
        {
            var beats = timestamps.ToList();
            var actions = new List<FunScriptAction>();

            TimeSpan previous = TimeSpan.MinValue;
            TimeSpan ramp = TimeSpan.FromMilliseconds(166);

            bool up = true;

            byte positionDown;
            byte positionUp;

            switch (mode)
            {
                case ConversionMode.UpOrDown:
                case ConversionMode.DownFast:
                case ConversionMode.DownCenter:
                    positionDown = 95;
                    positionUp = 5;
                    break;
                case ConversionMode.UpFast:
                case ConversionMode.UpCenter:
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
                        {
                            if (previous != TimeSpan.MinValue)
                            {
                                actions.Add(new FunScriptAction
                                {
                                    Position = positionDown,
                                    TimeStamp = (previous + timestamp).Divide(2)
                                });
                            }

                            actions.Add(new FunScriptAction
                            {
                                Position = positionUp,
                                TimeStamp = timestamp
                            });


                            break;
                        }
                    case ConversionMode.UpFast:
                    case ConversionMode.DownFast:
                        {
                            if (previous != TimeSpan.MinValue)
                            {
                                if (timestamp - previous >= ramp.Multiply(2))
                                {
                                    actions.Add(new FunScriptAction
                                    {
                                        Position = positionDown,
                                        TimeStamp = previous + ramp
                                    });

                                    actions.Add(new FunScriptAction
                                    {
                                        Position = positionDown,
                                        TimeStamp = timestamp - ramp
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
