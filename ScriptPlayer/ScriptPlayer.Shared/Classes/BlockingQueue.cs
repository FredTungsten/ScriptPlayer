using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ScriptPlayer.Shared
{
    public class BlockingQueue<T> where T : class
    {
        private ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        private readonly object _queueLock = new object();

        private bool _closed;

        public void Close()
        {
            try
            {
                if (_closed) return;

                lock (_queueLock)
                {
                    _closed = true;
                    Monitor.PulseAll(_queueLock);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public int Count
        {
            get
            {
                lock (_queueLock)
                    return _queue.Count;
            }
        }

        public void Enqueue(T item)
        {
            if (_closed) return;

            lock (_queueLock)
            {
                _queue.Enqueue(item);
                Monitor.Pulse(_queueLock);
            }
        }

        public void Prioritize(Comparison<T> comparison, Predicate<T> condition)
        {
            lock (_queueLock)
            {
                var allEntries = _queue.ToList();

                var toSort = allEntries.Where(e => condition(e)).ToList();

                foreach (T entry in toSort)
                {
                    allEntries.Remove(entry);
                }
                
                toSort.Sort(comparison);

                allEntries.InsertRange(0, toSort);

                _queue = new ConcurrentQueue<T>(allEntries);
            }
        }

        public T Dequeue(TimeSpan timeout)
        {
            try
            {
                if (_closed) return null;

                lock (_queueLock)
                {
                    T result;

                    while (!_queue.TryDequeue(out result))
                    {
                        if (_closed)
                            return null;

                        if (!Monitor.Wait(_queueLock, timeout))
                            return null;
                    }

                    return result;
                }
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }

        public T Dequeue()
        {
            try
            {
                if (_closed) return null;

                lock (_queueLock)
                {
                    T result;

                    while (!_queue.TryDequeue(out result))
                    {
                        if (_closed)
                            return null;

                        Monitor.Wait(_queueLock);
                    }

                    return result;
                }
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }

        private void DeleteTill(Func<T, T, bool> func, T comparison)
        {
            lock (_queueLock)
            {
                ConcurrentQueue<T> newQueue = new ConcurrentQueue<T>();

                while (_queue.Count > 0)
                {
                    _queue.TryDequeue(out T item);

                    if (func(item, comparison))
                        break;

                    newQueue.Enqueue(item);
                }

                _queue = newQueue;
            }
        }

        public void ReplaceExisting(T item, Func<T,T, bool> condition)
        {
            lock (_queueLock)
            {
                DeleteTill(condition, item);
                Enqueue(item);
            }
        }

        public void Clear()
        {
            lock (_queueLock)
            {
                while (!_queue.IsEmpty)
                    _queue.TryDequeue(out T _);
            }
        }
    }
}