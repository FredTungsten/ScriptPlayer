using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ScriptPlayer.Shared.Beats
{
    public class TactCollection : IList<Tact>
    {
        private List<Tact> _tacts = new List<Tact>();

        public IEnumerable<Tact> Get(TimeSpan from, TimeSpan to)
        {
            foreach (Tact tact in _tacts)
            {
                if (tact.Start <= to && tact.End >= from)
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
            _tacts.Add(item);
        }

        public void Clear()
        {
            _tacts.Clear();
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
            return _tacts.Remove(item);
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
            _tacts.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _tacts.RemoveAt(index);
        }

        public Tact this[int index]
        {
            get => _tacts[index];
            set => _tacts[index] = value;
        }
    }
}
