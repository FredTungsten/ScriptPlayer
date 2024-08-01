using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace ScriptPlayer.Shared.Beats
{
    public class BarCollection : IList<Bar>, ITickSource
    {
        public event EventHandler Changed;

        private List<Bar> _bars = new List<Bar>();

        public IEnumerator<Bar> GetEnumerator()
        {
            return _bars.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Bar item)
        {
            _bars.Add(item);
            Observe(item);
            OnChanged();
        }

        private void Observe(Bar item)
        {
            if (item == null) return;

            item.PropertyChanged += ItemOnPropertyChanged;
            item.TactChanged += ItemOnTactChanged;
        }

        private void Unobserve(Bar item)
        {
            if (item == null) return;

            item.PropertyChanged += ItemOnPropertyChanged;
            item.TactChanged += ItemOnTactChanged;
        }

        private void ItemOnTactChanged(object sender, EventArgs eventArgs)
        {
            OnChanged();
        }

        private void ItemOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            OnChanged();
        }

        public void Clear()
        {
            foreach (Bar bar in _bars)
            {
                Unobserve(bar);
            }

            _bars.Clear();
            OnChanged();
        }

        public bool Contains(Bar item)
        {
            return _bars.Contains(item);
        }

        public void CopyTo(Bar[] array, int arrayIndex)
        {
            _bars.CopyTo(array, arrayIndex);
        }

        public bool Remove(Bar item)
        {
            Unobserve(item);
            bool removed = _bars.Remove(item);
            if(removed)
                OnChanged();
            return removed;
        }

        public int Count
        {
            get => _bars.Count;
        }

        public bool IsReadOnly
        {
            get => false;
        }

        public int IndexOf(Bar item)
        {
            return _bars.IndexOf(item);
        }

        public void Insert(int index, Bar item)
        {
            _bars.Insert(index, item);
            Observe(item);
            OnChanged();
        }

        public void RemoveAt(int index)
        {
            Unobserve(_bars[index]);
            _bars.RemoveAt(index);
            OnChanged();
        }

        public Bar this[int index]
        {
            get => _bars[index];
            set
            {
                Unobserve(_bars[index]);
                _bars[index] = value;
                Observe(_bars[index]);
                OnChanged();
            }
        }

        protected virtual void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public TickType DetermineTick(TimeFrame timeframe)
        {
            foreach (Bar bar in _bars)
            {
                if (!bar.Intersects(timeframe))
                    continue;

                TickType type = bar.DetermineTick(timeframe);
                if (type != TickType.None)
                    return type;
            }

            return TickType.None;
        }

        public IEnumerable<Bar> Get(TimeSpan time)
        {
            foreach (Bar bar in _bars)
            {
                if (bar.GetStartTime() <= time && bar.GetEndTime() >= time)
                    yield return bar;
            }
        }
    }
}