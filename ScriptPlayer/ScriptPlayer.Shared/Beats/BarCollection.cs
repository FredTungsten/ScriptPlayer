using System.Collections;
using System.Collections.Generic;

namespace ScriptPlayer.Shared.Beats
{
    public class BarCollection : IList<Bar>
    {
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
        }

        public void Clear()
        {
            _bars.Clear();
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
            return _bars.Remove(item);
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
        }

        public void RemoveAt(int index)
        {
            _bars.RemoveAt(index);
        }

        public Bar this[int index]
        {
            get => _bars[index];
            set => _bars[index] = value;
        }
    }
}