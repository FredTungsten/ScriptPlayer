using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptPlayer.Shared.Scripts
{
    public class RealTouchScriptLoader : ScriptLoader
    {
        public override List<ScriptAction> Load(Stream stream)
        {
            List<RealTouchCommand> commands = new List<RealTouchCommand>();

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
            {

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    RealTouchCommand command = RealTouchCommand.Parse(line);
                    if (command != null)
                        commands.Add(command);
                }
            }

            List<RawScriptAction> intermediateResult = ConvertToRawFunscript(commands);
            List<FunScriptAction> finalResult = intermediateResult.Select(raw => new FunScriptAction
            {
                TimeStamp = raw.TimeStamp,
                Position = raw.Position
            }).ToList();

            //TODO Smooth movement (e.g. CH_Corruptors.ott @ 21:00)
            finalResult = RawScriptConverter.Convert(intermediateResult);

            return finalResult.Cast<ScriptAction>().ToList();
        }

        private static List<RawScriptAction> ConvertToRawFunscript(List<RealTouchCommand> allCommands)
        {
            List<RawScriptAction> actions = new List<RawScriptAction>();
            List<RealTouchCommand> filteredcommands = FilterAndSortCommands(allCommands, RealTouchAxis.BothBelts, TimeSpan.FromMilliseconds(100));
            List<RealTouchCommand> commands = RemoveRepeatingCommands(filteredcommands);

            for (int i = 0; i < commands.Count; i++)
            {
                TimeSpan nextCommand = TimeSpan.MaxValue;
                if (i < commands.Count - 1)
                    nextCommand = commands[i + 1].TimeStamp;

                if (commands[i] is RealTouchPeriodicMovementCommand periodicCommand)
                {
                    TimeSpan endOfCommand = EarlierTimestamp(periodicCommand.TimeStamp + periodicCommand.Duration, nextCommand);
                    RealTouchDirection direction = periodicCommand.Direction;
                    TimeSpan timestamp = periodicCommand.TimeStamp;
                    while (timestamp < endOfCommand)
                    {
                        actions.Add(new RawScriptAction
                        {
                            TimeStamp = timestamp,
                            Position = ConvertDirectionToPosition(direction),
                            Speed = ConvertMagnitudeToSpeed(periodicCommand.Magnitude)
                        });

                        direction = InvertDirection(direction);
                        timestamp += periodicCommand.Period;
                    }

                    actions.Add(new RawScriptAction
                    {
                        TimeStamp = endOfCommand,
                        Position = ConvertDirectionToPosition(direction),
                        Speed = ConvertMagnitudeToSpeed(periodicCommand.Magnitude)
                    });
                }
                else if (commands[i] is RealTouchVectorMovementCommand vectorCommand)
                {
                    TimeSpan endOfCommand = EarlierTimestamp(vectorCommand.TimeStamp + vectorCommand.Duration, nextCommand);
                    actions.Add(new RawScriptAction
                    {
                        TimeStamp = vectorCommand.TimeStamp,
                        Position = ConvertDirectionToPosition(vectorCommand.Direction),
                        Speed = ConvertMagnitudeToSpeed(vectorCommand.Magnitude)
                    });
                    actions.Add(new RawScriptAction
                    {
                        TimeStamp = endOfCommand,
                        Position = ConvertDirectionToPosition(InvertDirection(vectorCommand.Direction)),
                        Speed = ConvertMagnitudeToSpeed(vectorCommand.Magnitude)
                    });
                }
            }

            return actions;
        }

        private static List<RealTouchCommand> RemoveRepeatingCommands(List<RealTouchCommand> commands)
        {
            List<RealTouchCommand> result = new List<RealTouchCommand>();

            foreach (RealTouchCommand command in commands)
            {
                if (result.Count == 0)
                {
                    result.Add(command);
                    continue;
                }

                RealTouchCommand previous = result.Last();

                if (command.Continues(previous))
                {
                    result.Remove(previous);
                    result.Add(command.Merge(previous));
                }
                else
                {
                    result.Add(command);
                }
            }

            return result;
        }

        private static byte ConvertMagnitudeToSpeed(byte magnitude)
        {
            return (byte)Math.Min(99, Math.Max(0, 0.99 * (magnitude / 2.55)));
        }

        private static byte ConvertDirectionToPosition(RealTouchDirection direction)
        {
            switch (direction)
            {
                case RealTouchDirection.In:
                    return 0;
                case RealTouchDirection.Out:
                    return 99;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        private static RealTouchDirection InvertDirection(RealTouchDirection direction)
        {
            switch (direction)
            {
                case RealTouchDirection.In:
                    return RealTouchDirection.Out;
                case RealTouchDirection.Out:
                    return RealTouchDirection.In;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        private static TimeSpan EarlierTimestamp(TimeSpan timestampA, TimeSpan timestampB)
        {
            return timestampA < timestampB ? timestampA : timestampB;
        }

        private static List<RealTouchCommand> FilterAndSortCommands(List<RealTouchCommand> commands, RealTouchAxis axis, TimeSpan minPeriodicDuration)
        {
            List<RealTouchCommand> result = new List<RealTouchCommand>();

            foreach (RealTouchCommand command in commands)
            {
                if (!CoversAxis(command.Axis, axis))
                    continue;
                if (command is RealTouchPeriodicMovementCommand periodic)
                    if (periodic.Period < minPeriodicDuration)
                        continue;

                result.Add(command);
            }

            result.Sort((a,b) => a.TimeStamp.CompareTo(b.TimeStamp));

            return result;
        }

        private static bool CoversAxis(RealTouchAxis commandAxis, RealTouchAxis filterAxis)
        {
            if (filterAxis == RealTouchAxis.Unknown || commandAxis == RealTouchAxis.Unknown)
                return false;

            if (commandAxis == RealTouchAxis.All)
                return true;

            switch (filterAxis)
            {
                case RealTouchAxis.TopBelt:
                    return commandAxis == RealTouchAxis.TopBelt || commandAxis == RealTouchAxis.BothBelts;
                case RealTouchAxis.BottomBelt:
                    return commandAxis == RealTouchAxis.BottomBelt || commandAxis == RealTouchAxis.BothBelts;
                default:
                    return commandAxis == filterAxis;
            }
        }

        public abstract class RealTouchCommand
        {
            public TimeSpan TimeStamp { get; protected set; }

            public RealTouchAxis Axis { get; protected set; }

            public static RealTouchCommand Parse(string line)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(line))
                        return null;

                    string[] parts = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        return null;


                    TimeSpan timestamp = ParseDuration(parts[0]);

                    RealTouchCommand command = null;

                    switch (parts[1])
                    {
                        case "V":
                            command = new RealTouchVectorMovementCommand();
                            break;
                        case "P":
                            command = new RealTouchPeriodicMovementCommand();
                            break;
                        case "S":
                            command = new RealTouchStopCommand();
                            break;
                        default:
                            return null;
                    }

                    if (command.MinParametersCount > parts.Length - 2)
                        return null;

                    command.TimeStamp = timestamp;
                    command.Init(parts, 2);

                    return command;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Could not parse line '{0}': {1}", line, e.Message);
                    return null;
                }
            }

            protected static TimeSpan ParseDuration(string s)
            {
                long milliseconds = long.Parse(s, CultureInfo.InvariantCulture);
                return TimeSpan.FromMilliseconds(milliseconds);
            }

            public RealTouchAxis ParseAxis(string axis)
            {
                switch (axis.ToUpperInvariant())
                {
                    case "A":
                        return RealTouchAxis.All;
                    case "H":
                        return RealTouchAxis.Heater;
                    case "L":
                        return RealTouchAxis.Lube;
                    case "T":
                        return RealTouchAxis.TopBelt;
                    case "B":
                        return RealTouchAxis.BottomBelt;
                    case "U":
                        return RealTouchAxis.BothBelts;
                    case "S":
                        return RealTouchAxis.Squeeze;
                    default:
                        return RealTouchAxis.Unknown;
                }
            }

            public RealTouchDirection ParseDirection(string direction)
            {
                switch (direction.ToUpperInvariant())
                {
                    case "IN":
                        return RealTouchDirection.In;
                    case "OUT":
                        return RealTouchDirection.Out;
                    default:
                        return RealTouchDirection.Unknown;

                }
            }

            protected abstract void Init(string[] parameters, int startindex);
            protected abstract int MinParametersCount { get; }

            public bool Continues(RealTouchCommand last)
            {
                if (GetType() != last.GetType())
                    return false;

                return ContinuesInternal(last);
            }

            public abstract RealTouchCommand Merge(RealTouchCommand last);

            protected abstract bool ContinuesInternal(RealTouchCommand last);


        }

        [DebuggerDisplay("V @{TimeStamp}: {Axis}, {Direction}, {Magnitude}")]
        public class RealTouchVectorMovementCommand : RealTouchCommand
        {
            public byte Magnitude { get; protected set; }
            public RealTouchDirection Direction { get; protected set; }
            public TimeSpan Duration { get; protected set; }

            protected override void Init(string[] parameters, int startindex)
            {
                Magnitude = byte.Parse(parameters[startindex + 0]);
                Axis = ParseAxis(parameters[startindex + 1]);
                Direction = ParseDirection(parameters[startindex + 2]);
                Duration = ParseDuration(parameters[startindex + 3]);
            }

            protected override int MinParametersCount => 4;
            public override RealTouchCommand Merge(RealTouchCommand last)
            {
                RealTouchVectorMovementCommand previous = (RealTouchVectorMovementCommand)last;

                return new RealTouchVectorMovementCommand
                {
                    Axis = Axis,
                    Direction = Direction,
                    TimeStamp = previous.TimeStamp,
                    Duration = (TimeStamp + Duration) - previous.TimeStamp,
                    Magnitude = Magnitude
                };
            }

            protected override bool ContinuesInternal(RealTouchCommand last)
            {
                RealTouchVectorMovementCommand previous = (RealTouchVectorMovementCommand) last;

                if (previous.Direction != Direction)
                    return false;
                if (Math.Abs((previous.TimeStamp + previous.Duration - TimeStamp).TotalMilliseconds) > 50)
                    return false;
                if (Math.Abs(previous.Magnitude - Magnitude) > 10)
                    return false;

                return true;
            }
        }

        [DebuggerDisplay("P @{TimeStamp}: {Axis}, {Direction}, {Magnitude}")]
        public class RealTouchPeriodicMovementCommand : RealTouchVectorMovementCommand
        {
            public TimeSpan Period { get; protected set; }

            protected override void Init(string[] parameters, int startindex)
            {
                Period = ParseDuration(parameters[startindex]);
                base.Init(parameters, startindex + 1);
            }

            protected override int MinParametersCount => base.MinParametersCount + 1;
        }

        [DebuggerDisplay("S @{TimeStamp}: {Axis}")]
        public class RealTouchStopCommand : RealTouchCommand
        {
            protected override void Init(string[] parameters, int startindex)
            {
                Axis = ParseAxis(parameters[startindex]);
            }

            protected override int MinParametersCount => 1;
            public override RealTouchCommand Merge(RealTouchCommand last)
            {
                return new RealTouchStopCommand
                {
                    Axis = Axis,
                    TimeStamp = last.TimeStamp
                };
            }

            protected override bool ContinuesInternal(RealTouchCommand last)
            {
                return true;
            }
        }

        public override List<ScriptFileFormat> GetSupportedFormats()
        {
            return new List<ScriptFileFormat>
            {
                new ScriptFileFormat("RealTouch Script", "ott")
            };
        }


    }

    public enum RealTouchAxis
    {
        All,
        Heater,
        Lube,
        TopBelt,
        BottomBelt,
        BothBelts,
        Squeeze,
        Unknown
    }

    public enum RealTouchDirection
    {
        In,
        Out,
        Unknown
    }
}
