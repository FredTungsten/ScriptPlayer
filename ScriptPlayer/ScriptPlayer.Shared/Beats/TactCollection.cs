using System;
using System.Collections;
using System.Collections.Generic;

namespace ScriptPlayer.Shared.Beats
{
    public class TactCollection : IList<Tact>, ITickSource
    {
        public event EventHandler Changed;

        private List<Tact> _tacts = new List<Tact>();

        public IEnumerable<Tact> Get(TimeSpan from, TimeSpan to)
        {
            foreach (Tact tact in _tacts)
            {
                if (tact.Start <= to && tact.End >= from)
                    yield return tact;
            }
        }


        public IEnumerable<Tact> Get(TimeFrame timeframe)
        {
            foreach (Tact tact in _tacts)
            {
                if (tact.TimeFrame.Intersects(timeframe))
                    yield return tact;
            }
        }

        public IEnumerable<Tact> Get(TimeSpan time)
        {
            foreach (Tact tact in _tacts)
            {
                if (tact.Start <= time && tact.End >= time)
                    yield return tact;
            }
        }


        public IEnumerator<Tact> GetEnumerator()
        {
            return _tacts.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Tact item)
        {
            Observe(item);
            _tacts.Add(item);
            OnChanged();
        }

        private void Observe(Tact item)
        {
            item.PropertyChanged += Item_PropertyChanged;
        }

        private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnChanged();
        }

        public void Clear()
        {
            _tacts.ForEach(Unobserve);
            _tacts.Clear();
            OnChanged();
        }

        private void Unobserve(Tact item)
        {
            item.PropertyChanged -= Item_PropertyChanged;
        }

        public bool Contains(Tact item)
        {
            return _tacts.Contains(item);
        }

        public void CopyTo(Tact[] array, int arrayIndex)
        {
            _tacts.CopyTo(array, arrayIndex);
        }

        public bool Remove(Tact item)
        {
            Unobserve(item);
            bool removed = _tacts.Remove(item);
            if(removed)
                OnChanged();

            return removed;
        }

        public int Count
        {
            get => _tacts.Count;
        }

        public bool IsReadOnly
        {
            get => false;
        }

        public int IndexOf(Tact item)
        {
            return _tacts.IndexOf(item);
        }

        public void Insert(int index, Tact item)
        {
            Observe(item);
            _tacts.Insert(index, item);
            OnChanged();
        }

        public void RemoveAt(int index)
        {
            Unobserve(_tacts[index]);
            _tacts.RemoveAt(index);
            OnChanged();
        }

        public Tact this[int index]
        {
            get => _tacts[index];
            set
            {
                Observe(value);
                _tacts[index] = value;
                OnChanged();
            }
        }

        public TickType DetermineTick(TimeFrame within)
        {
            foreach (Tact tact in _tacts)
            {
                if (!tact.TimeFrame.Intersects(within))
                    continue;

                TickType tickType = tact.DetermineTick(within);
                if (tickType != TickType.None)
                    return tickType;
            }

            return TickType.None;
        }

        protected virtual void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

    }
}
