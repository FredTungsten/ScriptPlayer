using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace ScriptPlayer.Shared
{
    public class BlockingQueue<T> where T : class
    {
        private ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        private readonly object _queueLock = new object();

        private readonly ManualResetEvent _event = new ManualResetEvent(false);

        private bool _closed;

        public void Close()
        {
            _closed = true;
            _event.Set();
            Thread.Yield();
            _event.Set();
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
                _event.Set();
            }
        }

        public T Deqeue()
        {
            if (_closed) return null;

            T result;

            while (!_queue.TryDequeue(out result))
            {
                if (_closed) return null;

                while (_queue.IsEmpty)
                {
                    if (_closed) return null;

                    _event.WaitOne();
                    _event.Reset();
                }
            }

            return result;
        }

        private void DeleteTill(Func<T, bool> func)
        {
            lock (_queueLock)
            {
                ConcurrentQueue<T> newQueue = new ConcurrentQueue<T>();

                while (_queue.Count > 0)
                {
                    T item;
                    _queue.TryDequeue(out item);

                    if (func(item))
                        break;

                    newQueue.Enqueue(item);
                }

                _queue = newQueue;
            }
        }

        public void ReplaceExisting(T item, Func<T, bool> condition)
        {
            lock (_queueLock)
            {
                DeleteTill(condition);
                Enqueue(item);
            }
        }
    }
}