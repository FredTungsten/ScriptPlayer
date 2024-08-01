using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace ScriptPlayer.Shared.Beats
{
    public class Tact : INotifyPropertyChanged, ITickSource
    {
        private int _beats;
        private int _beatsPerBar;
        private TimeSpan _start;
        private TimeSpan _end;
        private TimeSpan _beatDuration;

        public TimeSpan Start
        {
            get => _start;
            set
            {
                if (value.Equals(_start)) return;
                _start = value;
                CalculateBeatDuration();
                OnPropertyChanged();
                OnPropertyChanged(nameof(Duration));
            }
        }

        public TimeSpan End
        {
            get => _end;
            set
            {
                if (value.Equals(_end)) return;
                _end = value;
                CalculateBeatDuration();
                OnPropertyChanged();
                OnPropertyChanged(nameof(Duration));
            }
        }

        public TimeSpan Duration
        {
            get => End - Start;
        }

        public TimeSpan BeatDuration
        {
            get => _beatDuration;
        }

        public int Beats
        {
            get => _beats;
            set
            {
                if (value == _beats) return;
                _beats = value;
                CalculateBeatDuration();
                OnPropertyChanged();
            }
        }

        public int BeatsPerBar
        {
            get => _beatsPerBar;
            set
            {
                if (value == _beatsPerBar) return;
                _beatsPerBar = value;
                OnPropertyChanged();
            }
        }

        public TimeFrame TimeFrame { get => new TimeFrame(Start, End); }

        private void CalculateBeatDuration()
        {
            _beatDuration = Duration.Divide(Beats - 1);
        }


        public IEnumerable<TimeSpan> GetBeats()
        {
            return GetBeatIndices().Select(TranslateIndex);
        }

        public IEnumerable<TimeSpan> GetBeats(TimeSpan from, TimeSpan to)
        {
            return GetBeatIndices(from, to).Select(TranslateIndex);
        }

        public TimeSpan TranslateIndex(int index)
        {
            return Start + _beatDuration.Multiply(index);
        }

        /// <summary>
        /// returns all beats included in the passed TimeFrame
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public IEnumerable<int> GetBeatIndices(TimeSpan from, TimeSpan to)
        {
            int firstBeat = (int)Math.Ceiling((from - Start).Divide(_beatDuration));
            int lastBeat = (int)Math.Floor((to - Start).Divide(_beatDuration));

            firstBeat = Math.Min(Beats - 1, Math.Max(0, firstBeat));
            lastBeat = Math.Min(Beats - 1, Math.Max(0, lastBeat));

            return Enumerable.Range(firstBeat, lastBeat - firstBeat + 1);
        }

        /// <summary>
        /// returns all beats included in the passed TimeFrame
        /// </summary>
        public IEnumerable<int> GetBeatIndices(TimeFrame within)
        {
            int firstBeat = (int)Math.Ceiling((within.From - Start).Divide(_beatDuration));
            int lastBeat = (int)Math.Floor((within.To - Start).Divide(_beatDuration));

            firstBeat = Math.Min(Beats - 1, Math.Max(0, firstBeat));
            lastBeat = Math.Min(Beats - 1, Math.Max(0, lastBeat));

            return Enumerable.Range(firstBeat, lastBeat - firstBeat + 1);
        }

        public IEnumerable<int> GetBeatIndices()
        {
            return Enumerable.Range(0, Beats + 1);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TickType DetermineTick(TimeFrame within)
        {
            var indices = GetBeatIndices(within);
            
            foreach (int index in indices)
            {
                TimeSpan time = TranslateIndex(index);
                if (!within.Contains(time))
                    continue;

                return index % BeatsPerBar == 0 ? TickType.Major : TickType.Minor;
            }

            return TickType.None;
        }

        public int GetClosestIndex(TimeSpan time)
        {
            double position = (time - Start).Divide(_beatDuration);
            int index = (int) Math.Round(position, MidpointRounding.AwayFromZero);

            if (index < 0)
                index = 0;
            else if (index > Beats)
                index = Beats;

            return index;
        }
    }
}