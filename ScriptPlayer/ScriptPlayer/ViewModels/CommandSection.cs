using System;
using System.Collections.Generic;
using System.Linq;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Classes;

namespace ScriptPlayer.ViewModels
{
    public class CommandSection : Section
    {
        public int CommandCount { get; }
        public double CommandsPerSecond { get; }

        public static new CommandSection Empty => new CommandSection(TimeSpan.Zero, TimeSpan.Zero, 1);

        public List<TimeSpan> Positions { get; set; }

        public CommandSection(List<TimedPosition> positions, bool savePositions)
        {
            Start = positions.First().TimeStamp;
            End = positions.Last().TimeStamp;
            Duration = End - Start;

            CommandCount = positions.Count;
            if (Duration <= TimeSpan.Zero)
                CommandsPerSecond = 0.0;
            else
                CommandsPerSecond = (CommandCount - 1) / Duration.TotalSeconds;

            if (savePositions)
                Positions = positions.Select(p => p.TimeStamp).ToList();
        }

        public CommandSection(List<TimeSpan> positions, bool savePositions)
        {
            Start = positions.First();
            End = positions.Last();
            Duration = End - Start;

            CommandCount = positions.Count;
            if (Duration <= TimeSpan.Zero)
                CommandsPerSecond = 0.0;
            else
                CommandsPerSecond = (CommandCount - 1) / Duration.TotalSeconds;

            if (savePositions)
                Positions = positions.ToList();
        }

        public CommandSection(TimeSpan start, TimeSpan end, int commandCount)
        {
            Start = start;
            End = end;
            Duration = End - Start;

            CommandCount = commandCount;
            if (Duration <= TimeSpan.Zero)
                CommandsPerSecond = 0.0;
            else
                CommandsPerSecond = (CommandCount - 1) / Duration.TotalSeconds;
        }
    }
}