using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ScriptPlayer.Shared
{
    public class PositionCollection : ICollection<TimedPosition>
    {
        private readonly List<TimedPosition> _positions;

        private static CultureInfo _culture = new CultureInfo("en-us");
        public PositionCollection()
        {
            _positions = new List<TimedPosition>();
        }

        public PositionCollection(IEnumerable<TimedPosition> beats)
        {
            if (beats == null)
            {
                _positions = new List<TimedPosition>();
            }
            else
            {
                _positions = new List<TimedPosition>(beats);
                _positions.Sort((a, b) => a.TimeStamp.CompareTo(b.TimeStamp));
            }
        }

        public TimedPosition this[int index]
        {
            get => _positions[index];
        }

        public bool Remove(TimedPosition item)
        {
            return _positions.Remove(item);
        }

        public int Count => _positions.Count;
        public bool IsReadOnly => false;

        public IEnumerable<TimedPosition> GetPositions(TimeSpan timestampFrom, TimeSpan timestampTo)
        {
            int minIndex = 0;
            int maxIndex = _positions.Count - 1;

            int leftBounds = FindLastEarlierThan(minIndex, maxIndex, timestampFrom);

            if (leftBounds != -1)
            {
                int rightBounds = FindFirstLaterThan(leftBounds, maxIndex, timestampTo);

                if (rightBounds != -1)
                    return _positions.Skip(leftBounds).Take(rightBounds - leftBounds + 1);
                else
                    return _positions.Skip(leftBounds);
            }
            else
            {
                int rightBounds = FindFirstLaterThan(0, maxIndex, timestampTo);

                if(rightBounds > -1)
                    return _positions.Take(rightBounds + 1);

                return _positions.ToList();
            }
        }

        private int FindFirstLaterThan(int minIndex, int maxIndex, TimeSpan timestampTo)
        {
            for (int i = minIndex; i <= maxIndex; i++)
            {
                if (_positions[i].TimeStamp >= timestampTo)
                    return i;
            }

            return -1;
        }

        private int FindLastEarlierThan(int minIndex, int maxIndex, TimeSpan t)
        {
            if (minIndex < 0 || minIndex > _positions.Count - 1) return -1;
            if (maxIndex < 0 || maxIndex > _positions.Count - 1) return -1;

            if (_positions[_positions.Count - 1].TimeStamp < t)
                return _positions.Count - 1;

            int l = minIndex;
            int r = maxIndex;

            while (l <= r)
            {
                int m = (l + r) / 2;

                if (_positions[m].TimeStamp > t)
                {
                    if (m > minIndex)
                        if (_positions[m - 1].TimeStamp <= t)
                            return m - 1;

                    r = m - 1;
                }
                else
                {
                    if (m < maxIndex)
                        if (_positions[m + 1].TimeStamp > t)
                            return m;

                    l = m + 1;
                }
            }

            //No element found
            return -1;
        }

        public void Add(TimedPosition beat)
        {
            if(_positions.Count == 0)
                _positions.Add(beat);
            else
            {
                int newIndex = FindLastEarlierThan(0, _positions.Count - 1, beat.TimeStamp);
                _positions.Insert(newIndex+1,beat);
            }
        }

        public void Clear()
        {
            _positions.Clear();
        }

        public bool Contains(TimedPosition item)
        {
            return _positions.Contains(item);
        }

        public void CopyTo(TimedPosition[] array, int arrayIndex)
        {
            _positions.CopyTo(array, arrayIndex);
        }

        public IEnumerator<TimedPosition> GetEnumerator()
        {
            return _positions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public PositionCollection Duplicate()
        {
            return new PositionCollection(_positions);
        }

        public PositionCollection Shift(TimeSpan timeSpan)
        {
            return new PositionCollection(_positions.Select(b => new TimedPosition
            {
                Position = b.Position,
                TimeStamp = b.TimeStamp.Add(timeSpan)
            }));
        }
    }
}