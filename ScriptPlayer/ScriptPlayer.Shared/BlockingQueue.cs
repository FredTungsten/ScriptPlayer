using System.Collections.Concurrent;
using System.Threading;

namespace ScriptPlayer.Shared
{
    public class BlockingQueue<T> where T : class
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

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
    }
}