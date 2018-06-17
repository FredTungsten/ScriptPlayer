using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ScriptPlayer.Shared.Scripts;

namespace ScriptPlayer.Shared
{
    public abstract class PatternGenerator
    {
        public class TimedPosition
        {
            public byte Position { get; set; }
            public TimeSpan Duration { get; set; }
        }

        public class PositionTransistion
        {
            public byte From { get; set; }
            public byte To { get; set; }
            public TimeSpan Duration { get; set; }
        }

        protected bool _stopped;

        public void Stop()
        {
            _stopped = true;
        }

        public abstract IEnumerator<PositionTransistion> Get();
    }

    public class RandomPatternGenerator : PatternGenerator
    {
        public override IEnumerator<PositionTransistion> Get()
        {
            bool up = true;
            Random rng = new Random();

            while (!_stopped)
            {
                byte previousPosition = (byte) (up ? 99 : 0);
                up ^= true;
                byte currentPosition = (byte)(up ? 99 : 0);

                TimeSpan duration = TimeSpan.FromMilliseconds(rng.Next(180, 750));

                yield return new PositionTransistion
                {
                    Duration = duration,
                    From = previousPosition,
                    To = currentPosition
                };

                Thread.Sleep(duration);
            }
        }
    }

    public class EasyGridPatternGenerator : PatternGenerator
    {
        private readonly RepeatablePattern _pattern;

        public EasyGridPatternGenerator(RepeatablePattern pattern, TimeSpan duration)
        {
            _pattern = pattern;

            Duration = duration;
        }

        public TimeSpan Duration { get; set; }

        public override IEnumerator<PositionTransistion> Get()
        {
            int index = 0;

            if (_pattern.Count == 0)
                yield break;

            while (!_stopped)
            {
                int currentIndex = index;
                int nextIndex = (index + 1) % _pattern.Count;

                TimeSpan duration = Duration.Multiply(_pattern[currentIndex].Duration);

                yield return new PositionTransistion
                {
                    Duration = duration,
                    From = _pattern[currentIndex].Position,
                    To = _pattern[nextIndex].Position
                };

                Thread.Sleep(duration);

                index = nextIndex;
            }
        }
    }

    public class RepeatingPatternGenerator : PatternGenerator
    {
        private readonly List<TimedPosition> _commands = new List<TimedPosition>();

        public void Add(byte position, TimeSpan duration)
        {
            _commands.Add(new TimedPosition
            {
                Duration = duration, Position = position
            });
        }

        public override IEnumerator<PositionTransistion> Get()
        {
            int index = 0;

            if (_commands.Count == 0)
                yield break;

            while (!_stopped)
            {
                int currentIndex = index;
                int nextIndex = (index + 1) % _commands.Count;

                yield return new PositionTransistion
                {
                    Duration = _commands[nextIndex].Duration,
                    From = _commands[currentIndex].Position,
                    To = _commands[nextIndex].Position
                };

                Thread.Sleep(_commands[nextIndex].Duration);

                index = nextIndex;
            }
        }
    }
}
