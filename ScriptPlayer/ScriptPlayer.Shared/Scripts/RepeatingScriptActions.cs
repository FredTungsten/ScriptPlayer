using System;
using System.Collections.Generic;
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
        public abstract List<FunScriptAction> Generate(TimeSpan duration);
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

        public override List<FunScriptAction> Generate(TimeSpan duration)
        {
            TimeSpan progress = TimeSpan.Zero;

            bool up = true;
            Random rng = new Random();

            List<FunScriptAction> result = new List<FunScriptAction>();
            
            while (progress < duration)
            {
                up ^= true;
                byte currentPosition = (byte)(rng.Next(0,50) + (up ? 49 : 0));
                TimeSpan nextDuration = TimeSpan.FromMilliseconds(rng.Next(180, 750));

                result.Add(new FunScriptAction
                {
                    OriginalAction = true,
                    Position = currentPosition,
                    TimeStamp = progress
                });

                progress += nextDuration;
            }

            return result;
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

        public override List<FunScriptAction> Generate(TimeSpan duration)
        {
            TimeSpan progress = TimeSpan.Zero;

            List<FunScriptAction> result = new List<FunScriptAction>();

            int index = 0;
            
            if (_pattern.Count == 0)
                return result;

            while (progress < duration)
            {
                TimeSpan nextDuration = Duration.Multiply(_pattern[index].Duration);
                
                result.Add(new FunScriptAction
                {
                    OriginalAction = true,
                    Position = _pattern[index].Position,
                    TimeStamp = progress
                });

                progress += nextDuration;
                index = (index + 1) % _pattern.Count;
            }

            return result;
        }
    }
}
